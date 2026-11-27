# Changelog

All notable changes to this package will be documented in this file.

## [1.2.0] - 2026-11-27

### Added
- Centralized ScriptableObject auto-loading (`Assets/Resources/TrackingLogConfig.asset`) via `TrackingConfigLoader` and zero-argument `TrackingLog.Initialize()`.
- One-click Editor menu `Window > Tracking Log SDK > Open or Create Config (ScriptableObject)` (`TrackingLogConfigMenu`) and quick button in Live Debugger.
- Optional standalone JSON configuration support via `TrackingConfigSerializer` (`ToJson` / `FromJson` / `CreateTemplateJson`), `TrackingLog.Initialize(TextAsset)`, `TrackingLog.InitializeFromJson(string)`, and `TrackingLog.InitializeFromJsonFile(string)`.
- `TrackingLogBootstrap` auto-loads `Resources/TrackingLogConfig.asset` when no explicit asset is dragged in.
- `Samples~/QuickStart/tracking_config.example.json`.

## [1.1.2] - 2026-11-27

### Fixed
- Lowered minimum supported Unity version in `package.json` from `2020.3` to `2019.4` (LTS).
- Added `#if UNITY_2020_1_OR_NEWER` compatibility fallback (`isNetworkError` / `isHttpError`) in `HttpLogTransport` so Unity 2019.4 builds cleanly without `UnityWebRequest.Result`.

## [1.1.1] - 2026-11-27

### Added
- MIT License (`LICENSE.md`) and full OpenUPM metadata in `package.json` (`license`, `licensesUrl`, `documentationUrl`, `changelogUrl`, `repository`, `author`).
- GitHub Actions release workflow (`.github/workflows/release.yml`) validating `package.json` version against `v*` tags and automatically creating GitHub Releases.
- Deterministic Unity `.meta` files across package assets (`Runtime/`, `Editor/`, `Tests/`, root metadata files) including WebGL-only plugin settings for `TrackingWebGL.jslib`.
- OpenUPM Scoped Registry installation and release guide in `README.md`.

## [1.1.0] - 2026-11-27

### Added
- Default production endpoint `https://bigame.ezwork.vn/logs` (`TrackingConfig.DefaultEndpointUrl`).
- `TrackingConfig.Collection` / `TrackingConfigAsset.Collection` for routing all logs of a game/lesson into a target MongoDB collection (e.g., `B1_Quang`), falling back to `SessionCollection` / `LevelCollection` when empty.
- Top-level `action` (`SessionAction` / `LevelAction`) and `user` fields in serialized JSON payloads matching the backend ingestion API contract (`game_id`, `collection`, `action`, `user`).
- `AutoGenerateAnonymousUserId` option in `UnityEnvironmentCollector` to generate and persist an anonymous `player_<id>` in `PlayerPrefs` when no user ID is set yet.

## [1.0.0] - 2026-11-27

### Added
- Core `TrackingLog` static API and testable pure-C# `TrackingEngine` for `session_track`, `level_track` (`start`, `retry`, `in_progress`, `end`), and custom events.
- Automatic common metadata attachment (`session_id`, `user_id`, `timestamp`, `game_id`, `game_version`, `build_id`, `platform`, `browser`, `device_model`, `os_version`, plus extensible custom metadata).
- Zero-dependency, IL2CPP/WebGL-safe `TrackingJsonWriter` and `TrackingPayloadBuilder` (`Flat` and `Wrapped` payload modes).
- Pluggable `ILogTransport` architecture with built-in `HttpLogTransport` (`UnityWebRequest` + WebGL `sendBeacon` / `fetch keepalive`), `ConsoleLogTransport`, `FileLogTransport`, `CompositeLogTransport`, and `InMemoryLogTransport`.
- Bounded `LogDispatchQueue` with exponential backoff retry and optional JSONL offline queue persistence.
- WebGL `.jslib` plugin (`TrackingWebGL.jslib`) for user-agent detection, `pagehide`/`beforeunload`/`visibilitychange` hooks, and unload-safe `sendBeacon`/`fetch` flushing.
- `TrackingConfigAsset` ScriptableObject and `TrackingLogBootstrap` MonoBehaviour for zero-code setup.
- Editor `Live Debugger` window (`Window > Tracking Log SDK > Live Debugger`) and NUnit EditMode test suite.
- `Samples~/QuickStart` generic demo script.
