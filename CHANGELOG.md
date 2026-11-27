# Changelog

All notable changes to this package will be documented in this file.

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
