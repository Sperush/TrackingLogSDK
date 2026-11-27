# Tracking Log SDK for Unity (`com.trackinglog.sdk`)

Zero-dependency Unity UPM package for standardized game telemetry (`session_track`, `level_track`, and custom events) across Desktop, Mobile, and WebGL.

---

## 1. Cài đặt (Installation)

### Cách A — Cài qua Unity Package Manager (Local Disk / Git URL)
1. Mở Unity → `Window` → `Package Manager`.
2. Nhấn nút `+` ở góc trên bên trái:
   - **Add package from disk...** → chọn file `package.json` trong thư mục `TrackingLogSDK`.
   - Hoặc **Add package from git URL...** → dán URL Git repo chứa package.

### Cách B — Thêm trực tiếp vào `Packages/manifest.json`
```json
{
  "dependencies": {
    "com.trackinglog.sdk": "file:../TrackingLogSDK"
  }
}
```

---

## 2. Cấu hình nhanh (Quick Setup)

### Cách 1: Không cần viết code khởi tạo (Inspector)
1. Trong Project window, chuột phải → `Create` → `Tracking Log SDK` → `Config` để tạo `TrackingLogConfig.asset`.
2. Điền `Endpoint Url`, `Game Id`, `Game Version`, `Build Id` và bật các transport mong muốn (`Enable Http Transport`, `Enable Console Transport`, `Enable File Transport`).
3. Gắn component `TrackingLogBootstrap` vào một `GameObject` ở scene đầu tiên và kéo `TrackingLogConfig.asset` vào ô **Config Asset**.

### Cách 2: Khởi tạo bằng code (C#)
```csharp
using System.Collections.Generic;
using TrackingLogSDK;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        var config = new TrackingConfig
        {
            EndpointUrl = TrackingConfig.DefaultEndpointUrl, // https://bigame.ezwork.vn/logs
            GameId = "physic_game",
            Collection = "B1_Quang",
            GameVersion = Application.version,
            BuildId = "build_01",
            InProgressIntervalSeconds = 5f,
            EnableHttpTransport = true,
            EnableConsoleTransport = true
        };

        TrackingLog.Initialize(config);
        TrackingLog.SetUserId("user_12345");
    }
}
```

---

## 3. API Lifecycle

### Session (`session_track`)
- Mặc định `AutoStartSession = true` sẽ tự động gửi `session_track` với `action_name = "start"` ngay khi `Initialize()` xong.
- Thời gian app ở background (`OnApplicationPause(true)` / mất focus / tab WebGL ẩn) được tự động loại trừ khỏi `duration`.
- Khi thoát game (`OnApplicationQuit` / WebGL `pagehide`/`beforeunload`), SDK tự động flush `level_track` (`action_type = "quit_app"`) nếu đang chơi dở và `session_track` (`action_name = "end"`).

### Level (`level_track`)
```csharp
// 1. Bắt đầu màn chơi (attempt_id tự tăng & lưu qua PlayerPrefs nếu PersistAttemptCounts = true)
TrackingLog.StartLevel("Lv_0001", "normal");

// 2. Đăng ký callback trả về snapshot result cho sự kiện in_progress mỗi 5 giây
TrackingLog.SetLevelResultProvider(() => new Dictionary<string, object>
{
    { "score", currentScore },
    { "hp", currentHp }
});

// 3. Ghi nhận sự kiện thành phần trong màn (tự gắn time_tick = số giây active từ lúc bắt đầu màn)
TrackingLog.TrackLevelEvent("checkpoint_reached", "cp_02");

// 4. Tạm dừng / Tiếp tục (thời gian pause không tính vào duration và time_tick)
TrackingLog.PauseLevel("menu");
TrackingLog.ResumeLevel();

// 5. Kết thúc màn chơi
// Thắng:
TrackingLog.EndLevelWin(new Dictionary<string, object>
{
    { "score", 1500 },
    { "stars", 3 }
});

// Hoặc Thua:
TrackingLog.EndLevelLose("out_of_time", finalResult);

// Hoặc Thoát giữa chừng (lose_reason tự động ép về null):
TrackingLog.QuitLevel(finalResult);

// Hoặc Chơi lại (tự đóng run cũ nếu còn mở và bắt đầu attempt mới với action_name = "retry"):
TrackingLog.RetryCurrentLevel();
```

### Custom Transport & Custom Event
```csharp
// Gửi event tự do (vẫn tự động đính kèm toàn bộ Common Metadata)
TrackingLog.TrackCustomEvent("tutorial_track", new Dictionary<string, object>
{
    { "step", 2 },
    { "action_name", "complete" }
});

// Cài đặt ILogTransport riêng (ví dụ: gửi qua JS PostMessage hoặc SDK bên thứ 3)
TrackingLog.Initialize(config, new MyCustomTransport());
```

---

## 4. Ví dụ Payload JSON chuẩn

### `session_track` (`start`)
```json
{
  "collection": "B1_Quang",
  "action": "session_track",
  "game_id": "physic_game",
  "user": "player_01",
  "session_id": "a1b2c3d4e5f6",
  "user_id": "player_01",
  "timestamp": 1795718133134,
  "game_version": "1.0.0",
  "build_id": "build_01",
  "platform": "Windows",
  "browser": "",
  "device_model": "PC",
  "os_version": "Windows 11",
  "action_name": "start",
  "start_time": 1795718133134,
  "end_time": null,
  "duration": 0
}
```

### `level_track` (`in_progress` mỗi 5s)
```json
{
  "collection": "B1_Quang",
  "action": "level_track",
  "game_id": "physic_game",
  "user": "player_01",
  "session_id": "a1b2c3d4e5f6",
  "user_id": "player_01",
  "timestamp": 1795718138134,
  "game_version": "1.0.0",
  "build_id": "build_01",
  "platform": "WebGL",
  "browser": "Chrome 130",
  "device_model": "Browser",
  "os_version": "Windows 10/11",
  "attempt_id": 1,
  "level_id": "Lv_0001",
  "start_time": 1795718133134,
  "end_time": 1795718138134,
  "duration": 5.012,
  "action_name": "in_progress",
  "action_type": null,
  "lose_reason": null,
  "level_type": "normal",
  "events": [
    {
      "event_name": "checkpoint_reached",
      "value": "cp_01",
      "time_tick": 3.42
    }
  ],
  "result": {
    "score": 250,
    "hp": 100
  }
}
```

---

## 5. WebGL & Live Debugger
- **WebGL**: Khi đóng tab hoặc chuyển tab (`pagehide`, `beforeunload`, `visibilitychange`), `.jslib` gọi thẳng về Unity và flush các gói log cuối thông qua `navigator.sendBeacon` (hoặc `fetch(..., { keepalive: true })` khi có custom headers/auth) để hạn chế mất log `quit_app` và `session_track end`.
- **Live Debugger**: Mở `Window` → `Tracking Log SDK` → `Live Debugger` trong Unity Editor để theo dõi trực tiếp session, level đang chạy, queue retry và sao chép JSON payload.
