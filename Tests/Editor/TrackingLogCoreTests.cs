using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace TrackingLogSDK.Tests
{
    internal sealed class FakeClock : IClock
    {
        public long UtcNowMs { get; set; }

        public FakeClock(long startMs = 1795718133000L)
        {
            UtcNowMs = startMs;
        }

        public void AdvanceSeconds(double seconds)
        {
            UtcNowMs += (long)(seconds * 1000.0);
        }
    }

    public sealed class TrackingLogCoreTests
    {
        private static TrackingConfig CreateBaseConfig()
        {
            return new TrackingConfig
            {
                GameId = "test_game",
                GameVersion = "1.2.3",
                BuildId = "build_99",
                InProgressIntervalSeconds = 5f,
                InProgressDeltaEventsOnly = true,
                AutoStartSession = false,
                AutoPauseOnFocusLost = true,
                AutoLogPauseEvents = false,
                MaxQueueSize = 100,
                MaxRetryCount = 3,
                InitialRetryDelaySeconds = 2f
            };
        }

        private static TrackingMetadata CreateBaseMetadata()
        {
            return new TrackingMetadata
            {
                UserId = "player_001",
                GameId = "test_game",
                GameVersion = "1.2.3",
                BuildId = "build_99",
                Platform = "Windows",
                Browser = "",
                DeviceModel = "TestPC",
                OsVersion = "Windows 11"
            };
        }

        [Test]
        public void JsonWriter_SerializesNullEscapingUtf8AndInvariantFloats()
        {
            CultureInfo previous = Thread.CurrentThread.CurrentCulture;
            try
            {
                // Ensure comma decimal locales (e.g., vi-VN, fr-FR) do not break JSON numbers.
                Thread.CurrentThread.CurrentCulture = new CultureInfo("vi-VN");

                var writer = new TrackingJsonWriter();
                writer.BeginObject();
                writer.WriteProperty("text", "Xin chào \"Unity\"\nLine2\\end\t\u0001");
                writer.WriteProperty("nullable", (string)null);
                writer.WriteProperty("ratio", 12.3456f, 2);
                writer.WriteProperty("nan", float.NaN);
                writer.WriteProperty("inf", double.PositiveInfinity);
                writer.WritePropertyValue("list", new object[] { 1, "two", true, null });
                writer.EndObject();

                string json = writer.ToString();
                Assert.IsTrue(json.Contains("\"text\":\"Xin chào \\\"Unity\\\"\\nLine2\\\\end\\t\\u0001\""));
                Assert.IsTrue(json.Contains("\"nullable\":null"));
                Assert.IsTrue(json.Contains("\"ratio\":12.35"));
                Assert.IsTrue(json.Contains("\"nan\":0"));
                Assert.IsTrue(json.Contains("\"inf\":0"));
                Assert.IsTrue(json.Contains("\"list\":[1,\"two\",true,null]"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [Test]
        public void SessionTrack_AttachesCommonMetadata_NoDuplicateSessionId_AndExcludesBackground()
        {
            var clock = new FakeClock(1795718133000L);
            var transport = new InMemoryLogTransport();
            var engine = new TrackingEngine(CreateBaseConfig(), CreateBaseMetadata(), transport, new InMemoryAttemptStore(), clock);
            engine.SetCustomMetadata("country", "VN");

            engine.StartSession();
            clock.AdvanceSeconds(4.0);
            engine.Tick(4.0f);

            engine.SetApplicationBackground(true);
            clock.AdvanceSeconds(10.0);
            engine.Tick(10.0f); // Should NOT count toward active session duration.

            engine.SetApplicationBackground(false);
            clock.AdvanceSeconds(2.5);
            engine.Tick(2.5f);

            engine.EndSession();

            Assert.AreEqual(2, transport.Sent.Count);

            string startJson = transport.Sent[0].JsonBody;
            Assert.IsTrue(startJson.Contains("\"collection\":\"session_track\""));
            Assert.IsTrue(startJson.Contains("\"action_name\":\"start\""));
            Assert.IsTrue(startJson.Contains("\"user_id\":\"player_001\""));
            Assert.IsTrue(startJson.Contains("\"country\":\"VN\""));
            Assert.IsTrue(startJson.Contains("\"end_time\":null"));
            Assert.IsTrue(startJson.Contains("\"duration\":0"));

            // Ensure session_id appears only once in flat JSON.
            int firstIdx = startJson.IndexOf("\"session_id\":", StringComparison.Ordinal);
            int secondIdx = startJson.IndexOf("\"session_id\":", firstIdx + 1, StringComparison.Ordinal);
            Assert.GreaterOrEqual(firstIdx, 0);
            Assert.AreEqual(-1, secondIdx);

            string endJson = transport.Sent[1].JsonBody;
            Assert.IsTrue(endJson.Contains("\"action_name\":\"end\""));
            Assert.IsTrue(endJson.Contains("\"duration\":6.5"));
            Assert.IsFalse(endJson.Contains("\"end_time\":null"));
        }

        [Test]
        public void LevelTrack_Lifecycle_StartInProgressDelta_PauseExcluded_EndFullEvents()
        {
            var clock = new FakeClock(1000000L);
            var transport = new InMemoryLogTransport();
            var cfg = CreateBaseConfig();
            cfg.AutoLogPauseEvents = true;
            var engine = new TrackingEngine(cfg, CreateBaseMetadata(), transport, new InMemoryAttemptStore(), clock);

            int score = 10;
            engine.StartSession();
            engine.StartLevel("Lv_0001", "special");
            engine.SetLevelResultProvider(() => new Dictionary<string, object> { { "score", score } });

            // Verify start payload
            string startJson = transport.Sent[1].JsonBody;
            Assert.IsTrue(startJson.Contains("\"attempt_id\":1"));
            Assert.IsTrue(startJson.Contains("\"level_id\":\"Lv_0001\""));
            Assert.IsTrue(startJson.Contains("\"level_type\":\"special\""));
            Assert.IsTrue(startJson.Contains("\"action_name\":\"start\""));
            Assert.IsTrue(startJson.Contains("\"action_type\":null"));
            Assert.IsTrue(startJson.Contains("\"lose_reason\":null"));
            Assert.IsTrue(startJson.Contains("\"end_time\":null"));
            Assert.IsTrue(startJson.Contains("\"events\":[]"));
            Assert.IsTrue(startJson.Contains("\"result\":{}"));

            // Advance 2s, record event 1
            clock.AdvanceSeconds(2.0);
            engine.Tick(2.0f);
            engine.TrackLevelEvent("checkpoint", "cp1");

            // Pause for 8s (must NOT advance duration or trigger in_progress)
            engine.PauseLevel("menu");
            clock.AdvanceSeconds(8.0);
            engine.Tick(8.0f);
            Assert.AreEqual(2, transport.Sent.Count);

            // Resume and advance 3s -> total active 5s -> triggers first in_progress
            engine.ResumeLevel();
            score = 50;
            clock.AdvanceSeconds(3.0);
            engine.Tick(3.0f);

            Assert.AreEqual(3, transport.Sent.Count);
            string inProg1 = transport.Sent[2].JsonBody;
            Assert.IsTrue(inProg1.Contains("\"action_name\":\"in_progress\""));
            Assert.IsTrue(inProg1.Contains("\"duration\":5"));
            Assert.IsTrue(inProg1.Contains("\"event_name\":\"checkpoint\""));
            Assert.IsTrue(inProg1.Contains("\"event_name\":\"pause\""));
            Assert.IsTrue(inProg1.Contains("\"event_name\":\"resume\""));
            Assert.IsTrue(inProg1.Contains("\"score\":50"));

            // Next 5s window: record event 2, should ONLY contain delta event in in_progress #2
            clock.AdvanceSeconds(2.0);
            engine.Tick(2.0f);
            engine.TrackLevelEvent("collect_item", "gem");
            score = 120;
            clock.AdvanceSeconds(3.0);
            engine.Tick(3.0f);

            Assert.AreEqual(4, transport.Sent.Count);
            string inProg2 = transport.Sent[3].JsonBody;
            Assert.IsTrue(inProg2.Contains("\"duration\":10"));
            Assert.IsTrue(inProg2.Contains("\"event_name\":\"collect_item\""));
            Assert.IsFalse(inProg2.Contains("\"event_name\":\"checkpoint\""));
            Assert.IsTrue(inProg2.Contains("\"score\":120"));

            // End with win -> must contain ALL events from the run
            engine.EndLevelWin(new Dictionary<string, object> { { "stars", 3 } });
            Assert.AreEqual(5, transport.Sent.Count);
            string endJson = transport.Sent[4].JsonBody;
            Assert.IsTrue(endJson.Contains("\"action_name\":\"end\""));
            Assert.IsTrue(endJson.Contains("\"action_type\":\"win\""));
            Assert.IsTrue(endJson.Contains("\"lose_reason\":null"));
            Assert.IsTrue(endJson.Contains("\"event_name\":\"checkpoint\""));
            Assert.IsTrue(endJson.Contains("\"event_name\":\"collect_item\""));
            Assert.IsTrue(endJson.Contains("\"score\":120"));
            Assert.IsTrue(endJson.Contains("\"stars\":3"));
        }

        [Test]
        public void LevelTrack_LoseQuitAndRetry_EnforcesLoseReasonAndIncrementsAttemptAcrossReload()
        {
            var clock = new FakeClock(2000000L);
            var transport = new InMemoryLogTransport();
            var sharedAttemptStore = new InMemoryAttemptStore();

            var engine1 = new TrackingEngine(CreateBaseConfig(), CreateBaseMetadata(), transport, sharedAttemptStore, clock);
            engine1.StartSession();
            engine1.StartLevel(1, "normal");
            engine1.EndLevelLose("player_dead");

            string loseJson = transport.Sent[2].JsonBody;
            Assert.IsTrue(loseJson.Contains("\"level_id\":1"));
            Assert.IsTrue(loseJson.Contains("\"attempt_id\":1"));
            Assert.IsTrue(loseJson.Contains("\"action_type\":\"lose\""));
            Assert.IsTrue(loseJson.Contains("\"lose_reason\":\"player_dead\""));

            // Calling EndLevel("quit_level", "ignored_reason") must force lose_reason = null
            engine1.StartLevel(1, "normal");
            engine1.EndLevel("quit_level", "should_be_nulled");
            string quitJson = transport.Sent[4].JsonBody;
            Assert.IsTrue(quitJson.Contains("\"attempt_id\":2"));
            Assert.IsTrue(quitJson.Contains("\"action_type\":\"quit_level\""));
            Assert.IsTrue(quitJson.Contains("\"lose_reason\":null"));

            // Simulate page reload with a new TrackingEngine sharing the same IAttemptStore
            var engine2 = new TrackingEngine(CreateBaseConfig(), CreateBaseMetadata(), transport, sharedAttemptStore, clock);
            engine2.StartSession();
            engine2.RetryLevel(1, "normal");

            string retryJson = transport.Sent[6].JsonBody;
            Assert.IsTrue(retryJson.Contains("\"attempt_id\":3"));
            Assert.IsTrue(retryJson.Contains("\"action_name\":\"retry\""));
        }

        [Test]
        public void UserAgentParser_DetectsMajorBrowsersAndPlatforms()
        {
            ParsedUserAgent edge = UserAgentParser.Parse(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36 Edg/130.0.2849.46");
            Assert.AreEqual("Edge 130.0", edge.Browser);
            Assert.AreEqual("Windows", edge.Platform);
            Assert.AreEqual("Windows 10/11", edge.OsVersion);

            ParsedUserAgent chromeMac = UserAgentParser.Parse(
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
            Assert.AreEqual("Chrome 128.0", chromeMac.Browser);
            Assert.AreEqual("MacOS", chromeMac.Platform);
            Assert.AreEqual("macOS 14.5", chromeMac.OsVersion);

            ParsedUserAgent safariIos = UserAgentParser.Parse(
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Mobile/15E148 Safari/604.1");
            Assert.AreEqual("Safari 17.4", safariIos.Browser);
            Assert.AreEqual("iOS", safariIos.Platform);
            Assert.AreEqual("iOS 17.4", safariIos.OsVersion);

            ParsedUserAgent firefoxUbuntu = UserAgentParser.Parse(
                "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:129.0) Gecko/20100101 Firefox/129.0");
            Assert.AreEqual("Firefox 129.0", firefoxUbuntu.Browser);
            Assert.AreEqual("Ubuntu", firefoxUbuntu.Platform);

            ParsedUserAgent operaAndroid = UserAgentParser.Parse(
                "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Mobile Safari/537.36 OPR/83.0");
            Assert.AreEqual("Opera 83.0", operaAndroid.Browser);
            Assert.AreEqual("Android", operaAndroid.Platform);
            Assert.AreEqual("Android 14", operaAndroid.OsVersion);
        }

        [Test]
        public void DispatchQueue_RetriesWithExponentialBackoff_AndPersistsOffline()
        {
            var clock = new FakeClock(10000L);
            var transport = new InMemoryLogTransport();
            transport.SimulateFailures(2, retryable: true);

            string tempFile = Path.Combine(Path.GetTempPath(), "tracking_queue_test_" + Guid.NewGuid().ToString("N") + ".jsonl");
            try
            {
                var storage = new FileLogQueueStorage(tempFile);
                var queue = new LogDispatchQueue(transport, clock, maxQueueSize: 5, maxRetryCount: 3, initialRetryDelaySeconds: 2f, storage: storage);

                queue.Enqueue(new TrackingEnvelope
                {
                    Collection = "level_track",
                    ActionName = "start",
                    JsonBody = "{\"ok\":1}",
                    EnqueuedAtMs = clock.UtcNowMs,
                    NextAttemptAtMs = clock.UtcNowMs
                });

                // First failure -> retry #1 scheduled at +2s (12000ms)
                Assert.AreEqual(1, queue.PendingCount);
                Assert.AreEqual(0, transport.Sent.Count);

                // Simulate reload from disk before backoff finishes
                var restoredQueue = new LogDispatchQueue(transport, clock, maxQueueSize: 5, maxRetryCount: 3, initialRetryDelaySeconds: 2f, storage: new FileLogQueueStorage(tempFile));
                Assert.AreEqual(1, restoredQueue.PendingCount);

                // Tick after 2s -> second simulated failure -> RetryCount becomes 2, backoff = 2 * 2^(2-1) = 4s
                clock.AdvanceSeconds(2.0);
                restoredQueue.Tick(clock.UtcNowMs);
                Assert.AreEqual(1, restoredQueue.PendingCount);

                // Tick after 2s (< 4s backoff) -> still waiting
                clock.AdvanceSeconds(2.0);
                restoredQueue.Tick(clock.UtcNowMs);
                Assert.AreEqual(1, restoredQueue.PendingCount);

                // Tick after another 2s (total 4s) -> succeeds!
                clock.AdvanceSeconds(2.0);
                restoredQueue.Tick(clock.UtcNowMs);
                Assert.AreEqual(0, restoredQueue.PendingCount);
                Assert.AreEqual(1, transport.Sent.Count);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Test]
        public void HandleApplicationQuit_IsIdempotent_AndFlushesQuitAppAndSessionEnd()
        {
            var clock = new FakeClock(500000L);
            var transport = new InMemoryLogTransport();
            var engine = new TrackingEngine(CreateBaseConfig(), CreateBaseMetadata(), transport, new InMemoryAttemptStore(), clock);

            engine.StartSession();
            engine.StartLevel("Lv_01", "normal");
            clock.AdvanceSeconds(3.0);
            engine.Tick(3.0f);

            engine.HandleApplicationQuit();
            engine.HandleApplicationQuit(); // Second call must be a no-op.

            Assert.AreEqual(4, transport.Sent.Count);
            Assert.IsTrue(transport.Sent[2].JsonBody.Contains("\"action_name\":\"end\""));
            Assert.IsTrue(transport.Sent[2].JsonBody.Contains("\"action_type\":\"quit_app\""));
            Assert.IsTrue(transport.Sent[2].JsonBody.Contains("\"lose_reason\":null"));
            Assert.IsTrue(transport.Sent[3].JsonBody.Contains("\"collection\":\"session_track\""));
            Assert.IsTrue(transport.Sent[3].JsonBody.Contains("\"action_name\":\"end\""));
        }

        [Test]
        public void WrappedPayloadAndCustomEvents_SerializeExpectedStructure()
        {
            var clock = new FakeClock(700000L);
            var transport = new InMemoryLogTransport();
            TrackingConfig cfg = CreateBaseConfig();
            cfg.PayloadFormat = PayloadFormat.Wrapped;
            cfg.WrappedDataField = "payload";
            cfg.IncludeCollectionField = false;

            var engine = new TrackingEngine(cfg, CreateBaseMetadata(), transport, new InMemoryAttemptStore(), clock);
            engine.TrackCustomEvent("tutorial_track", new Dictionary<string, object>
            {
                { "step", 2 },
                { "action_name", "complete" }
            }, "complete");

            Assert.AreEqual(1, transport.Sent.Count);
            string json = transport.Sent[0].JsonBody;
            Assert.IsFalse(json.Contains("\"collection\":"));
            Assert.IsTrue(json.Contains("\"payload\":{\"step\":2,\"action_name\":\"complete\"}"));
        }

        [Test]
        public void BackendContract_UsesDefaultEndpoint_CollectionActionAndUserFields()
        {
            var defaultConfig = new TrackingConfig();
            Assert.AreEqual("https://bigame.ezwork.vn/logs", defaultConfig.EndpointUrl);

            var clock = new FakeClock(1795718133000L);
            var transport = new InMemoryLogTransport();
            TrackingConfig cfg = CreateBaseConfig();
            cfg.GameId = "physic_game";
            cfg.Collection = "B1_Quang";

            TrackingMetadata meta = CreateBaseMetadata();
            meta.GameId = "physic_game";
            meta.UserId = "student_01";

            var engine = new TrackingEngine(cfg, meta, transport, new InMemoryAttemptStore(), clock);
            engine.StartSession();
            engine.StartLevel("Lv_01", "normal");

            Assert.AreEqual(2, transport.Sent.Count);

            string sessionJson = transport.Sent[0].JsonBody;
            Assert.AreEqual("B1_Quang", transport.Sent[0].Collection);
            Assert.IsTrue(sessionJson.Contains("\"game_id\":\"physic_game\""));
            Assert.IsTrue(sessionJson.Contains("\"collection\":\"B1_Quang\""));
            Assert.IsTrue(sessionJson.Contains("\"action\":\"session_track\""));
            Assert.IsTrue(sessionJson.Contains("\"user\":\"student_01\""));
            Assert.IsTrue(sessionJson.Contains("\"user_id\":\"student_01\""));
            Assert.IsTrue(sessionJson.Contains("\"action_name\":\"start\""));

            string levelJson = transport.Sent[1].JsonBody;
            Assert.AreEqual("B1_Quang", transport.Sent[1].Collection);
            Assert.IsTrue(levelJson.Contains("\"game_id\":\"physic_game\""));
            Assert.IsTrue(levelJson.Contains("\"collection\":\"B1_Quang\""));
            Assert.IsTrue(levelJson.Contains("\"action\":\"level_track\""));
            Assert.IsTrue(levelJson.Contains("\"user\":\"student_01\""));
            Assert.IsTrue(levelJson.Contains("\"action_name\":\"start\""));

            // When Collection is empty, falls back to SessionCollection / LevelCollection while still emitting action.
            cfg.Collection = string.Empty;
            var fallbackTransport = new InMemoryLogTransport();
            var fallbackEngine = new TrackingEngine(cfg, meta, fallbackTransport, new InMemoryAttemptStore(), clock);
            fallbackEngine.StartSession();
            fallbackEngine.StartLevel(1, "normal");

            Assert.IsTrue(fallbackTransport.Sent[0].JsonBody.Contains("\"collection\":\"session_track\""));
            Assert.IsTrue(fallbackTransport.Sent[0].JsonBody.Contains("\"action\":\"session_track\""));
            Assert.IsTrue(fallbackTransport.Sent[1].JsonBody.Contains("\"collection\":\"level_track\""));
            Assert.IsTrue(fallbackTransport.Sent[1].JsonBody.Contains("\"action\":\"level_track\""));
        }

        [Test]
        public void ConfigSerializer_PartialJsonPreservesDefaults_AndFullRoundTripWorks()
        {
            string partialJson = "{\n"
                + "  \"game_id\": \"physic_game\",\n"
                + "  \"collection\": \"B1_Quang\",\n"
                + "  \"build_id\": \"qa_02\",\n"
                + "  \"enable_console_transport\": true\n"
                + "}";

            TrackingConfig partial = TrackingConfigSerializer.FromJson(partialJson);
            Assert.AreEqual(TrackingConfig.DefaultEndpointUrl, partial.EndpointUrl);
            Assert.AreEqual("physic_game", partial.GameId);
            Assert.AreEqual("B1_Quang", partial.Collection);
            Assert.AreEqual("qa_02", partial.BuildId);
            Assert.IsTrue(partial.EnableHttpTransport);
            Assert.IsTrue(partial.EnableConsoleTransport);
            Assert.AreEqual(5f, partial.InProgressIntervalSeconds);

            var full = new TrackingConfig
            {
                EndpointUrl = "https://bigame.ezwork.vn/logs",
                ApiKey = "secret\"key",
                ApiKeyHeader = "X-Custom-Key",
                BearerToken = "tok_123",
                RequestTimeoutSeconds = 15,
                GameId = "physic_game",
                Collection = "B1_Quang",
                GameVersion = "2.1.0",
                BuildId = "rel_10",
                PayloadFormat = PayloadFormat.Wrapped,
                WrappedDataField = "payload",
                IncludeActionField = false,
                IncludeUserField = true,
                AutoGenerateAnonymousUserId = false,
                InProgressIntervalSeconds = 7.5f,
                MaxQueueSize = 250,
                MaxRetryCount = 4,
                InitialRetryDelaySeconds = 1.5f
            };
            full.CustomHeaders["X-Env"] = "staging";

            string json = TrackingConfigSerializer.ToJson(full, prettyPrint: true);
            TrackingConfig roundTrip = TrackingConfigSerializer.FromJson(json);

            Assert.AreEqual("https://bigame.ezwork.vn/logs", roundTrip.EndpointUrl);
            Assert.AreEqual("secret\"key", roundTrip.ApiKey);
            Assert.AreEqual("X-Custom-Key", roundTrip.ApiKeyHeader);
            Assert.AreEqual("tok_123", roundTrip.BearerToken);
            Assert.AreEqual(15, roundTrip.RequestTimeoutSeconds);
            Assert.AreEqual("physic_game", roundTrip.GameId);
            Assert.AreEqual("B1_Quang", roundTrip.Collection);
            Assert.AreEqual("2.1.0", roundTrip.GameVersion);
            Assert.AreEqual("rel_10", roundTrip.BuildId);
            Assert.AreEqual(PayloadFormat.Wrapped, roundTrip.PayloadFormat);
            Assert.AreEqual("payload", roundTrip.WrappedDataField);
            Assert.IsFalse(roundTrip.IncludeActionField);
            Assert.IsTrue(roundTrip.IncludeUserField);
            Assert.IsFalse(roundTrip.AutoGenerateAnonymousUserId);
            Assert.AreEqual(7.5f, roundTrip.InProgressIntervalSeconds);
            Assert.AreEqual(250, roundTrip.MaxQueueSize);
            Assert.AreEqual(4, roundTrip.MaxRetryCount);
            Assert.AreEqual(1.5f, roundTrip.InitialRetryDelaySeconds);
            Assert.AreEqual("staging", roundTrip.CustomHeaders["X-Env"]);
        }
    }
}
