# Plan: Android Companion Features in TimeCheck (MAUI MVP)

**Date:** April 2, 2026  
**Status:** Planning  
**Branch for implementation:** `feature/maui-android-companion`  
**Parent research:** `research android mobile interaction.md`

---

## Goal

Extend the existing `TimeCheck` .NET MAUI app so it can act as an Android companion client that proves two things:

1. **Hands-free command entry** — the user can speak a command without manually opening Telegram and tapping the mic each time.
2. **Basic phone control** — the assistant can open apps, open URLs, and perform simple global actions on the Android device.

Success criteria: from a persistent notification or Quick Settings tile, the user can trigger voice capture, send the command to the assistant API, receive a structured response, and have the Android device execute the returned action with minimal screen interaction.

This MVP is also a feasibility check for a larger idea: if command entry, action routing, and Android accessibility integration prove reliable, the same foundation could later evolve toward on-device dictation and limited voice-driven phone control. That said, replacing Android Voice Access is explicitly **not** the MVP target.

---

## Why This Repo

This plan is intentionally **not** a new Kotlin app. It reuses the existing `TimeCheck` MAUI application because:

- The app already exists and builds as a MAUI single-project app.
- Android-specific code can live under `Platforms/Android` without affecting Windows behavior.
- Existing app structure, packaging, resources, and deployment setup are already in place.
- This avoids rebuilding an app shell, app identity, permissions, and platform plumbing from scratch.

This means the Android companion work becomes a new feature area inside `TimeCheck`, while keeping the Windows-oriented time/cycling functionality intact unless explicitly refactored later.

---

## Repository Context

- Current app repo: `c:\Users\MPhil\source\repos\TimeCheck\TimeCheck`
- MAUI app project: `c:\Users\MPhil\source\repos\TimeCheck\TimeCheck\TimeCheck`
- Sibling assistant repo: `c:\Users\MPhil\source\repos\personal-assistant`

When testing the API locally, assume the assistant service can be run from the sibling `personal-assistant` repo and the Android device or emulator will call its `/api/command` endpoint over LAN or emulator loopback configuration.

---

## Constraints

- **No root required.** Must work on stock Android 10+.
- **No ADB dependency for end users.** ADB is acceptable for local debugging only.
- **Use this MAUI app, not a new Android project.** Android-only functionality should be isolated behind platform-specific services.
- **Minimal scope.** Only implement what is needed to prove feasibility.
- **Preserve existing app behavior where practical.** New companion features should not break current Windows usage.

---

## Architecture Overview

```text
┌──────────────────────────────────────┐
│ TimeCheck (.NET MAUI single project) │
│                                      │
│  Shared MAUI UI / settings           │
│  - Assistant endpoint config         │
│  - Device token config               │
│  - Status / debug surface            │
│                                      │
│  Android-only services               │
│  - Speech capture                    │
│  - Quick Settings tile               │
│  - Foreground notification           │
│  - Accessibility service             │
│  - Intent / media / navigation exec  │
│                                      │
│  HTTP client                         │
│  - POST /api/command                 │
└──────────────────┬───────────────────┘
                   │
                   ▼
┌──────────────────────────────────────┐
│ personal-assistant (.NET)            │
│ sibling repo at same directory level │
│                                      │
│ POST /api/command                    │
│ validates token                      │
│ returns text + action JSON           │
└──────────────────────────────────────┘
```

For the MVP, the communication channel remains a simple HTTP request/response flow.

---

## Shared Command Schema

Keep the schema stable so the app and assistant repo agree on the same action contract.

| Command | Parameters | MVP priority |
|---|---|---|
| `device.open_app` | `name: string` | Must have |
| `device.open_url` | `url: string` | Must have |
| `device.navigate` | `action: back \| home \| recents \| notifications` | Must have |
| `device.scroll` | `direction: up \| down \| left \| right` | Should have |
| `device.media` | `action: play \| pause \| next \| previous` | Should have |
| `device.tap_text` | `text: string` | Post-MVP |
| `device.type_text` | `text: string` | Post-MVP |

---

## MAUI Design Approach

The implementation should split responsibilities like this:

- **Shared MAUI layer**
  - Settings page for assistant URL and device token.
  - Shared models for request/response payloads.
  - Shared HTTP client wrapper and orchestration service.
  - Optional debug/status UI.

- **Android-specific layer**
  - Speech recognition entry point.
  - Foreground service and persistent notification.
  - Quick Settings tile.
  - Accessibility service for navigation and scroll.
  - Android intent launching and media key dispatch.

- **Service abstraction layer**
  - Shared interfaces live in the main app or `TimeCheck.Shared`.
  - Android implementations are registered conditionally in `MauiProgram.cs`.
  - Windows can use no-op or existing implementations so the app still runs there.

---

## Proposed File Layout

This is the most natural fit for the current repo structure.

```text
TimeCheck/
├── Models/
│   ├── CommandRequest.cs
│   ├── CommandResponse.cs
│   ├── DeviceAction.cs
│   └── ActionExecutionResult.cs
├── Services/
│   ├── AssistantApiClient.cs
│   ├── SettingsService.cs
│   ├── IActionExecutor.cs
│   ├── ICommandCaptureService.cs
│   └── IAccessibilityCommandService.cs
├── Pages/
│   └── CompanionSettingsPage.xaml(.cs)   [optional if MainPage becomes crowded]
├── Platforms/Android/
│   ├── MainActivity.cs
│   ├── CommandTileService.cs
│   ├── CommandForegroundService.cs
│   ├── SpeechCaptureActivity.cs
│   ├── CompanionAccessibilityService.cs
│   ├── AndroidActionExecutor.cs
│   ├── AndroidSpeechCaptureService.cs
│   ├── AndroidAppLauncher.cs
│   └── Resources/xml/accessibility_service_config.xml
└── Resources/
    └── images / icons as needed
```

If the shared models are likely to be reused by `TimeCheck.Web` or other components, they can move into `TimeCheck.Shared` instead.

---

## Phase 0 — Refactor for MAUI Companion Support

### Task 0.1: Confirm Android target and restore state
- Verify `net10.0-android` remains enabled in `TimeCheck.csproj`.
- Fix any package restore or PackageSourceMapping issues that block Android builds.
- Confirm the app deploys to an emulator or physical Android device before adding features.

### Task 0.2: Add companion settings to the existing app
- Add fields for:
  - Assistant base URL
  - Device token
  - Optional device display name
- Store settings using MAUI `Preferences` initially.
- If needed later, move token storage to secure storage.

### Task 0.3: Add shared command models and HTTP client
- Create `CommandRequest`, `CommandResponse`, and `DeviceAction` models in C#.
- Add an `AssistantApiClient` using `HttpClient`.
- Endpoint contract:

```csharp
POST /api/command
{
  "command": "open youtube",
  "deviceToken": "xxx"
}
```

- Response contract:

```json
{
  "actions": [
    { "type": "device.open_app", "params": { "name": "youtube" } }
  ],
  "textResponse": "Opening YouTube"
}
```

### Deliverable
- The MAUI app can save endpoint settings and successfully call the assistant API from Android.

---

## Phase 1 — Hands-Free Command Entry in MAUI + Android

### Task 1.1: Add speech capture service abstraction
- Define `ICommandCaptureService` in shared code.
- Android implementation should use Android speech recognition APIs.
- Keep the shared UI unaware of the Android-specific speech API details.

### Task 1.2: Add Quick Settings tile
- Create an Android `TileService` under `Platforms/Android`.
- On tile tap:
  1. Launch an Android activity dedicated to speech capture.
  2. Capture speech.
  3. POST text to the assistant API.
  4. Show a toast or notification update with the response text.
  5. Forward actions to the Android action executor.

### Task 1.3: Add persistent notification trigger
- Create an Android foreground service.
- Show a persistent notification with a `Speak Command` action.
- The action should trigger the same speech-to-command pipeline as the tile.

### Task 1.4: Decide whether MainPage hosts companion controls
- Short term: add a simple companion settings/status section to the existing `MainPage`.
- If `MainPage` becomes too mixed, split companion settings into a dedicated page.

### Deliverable
- From Android, the user can trigger voice capture from a tile or persistent notification and receive a server response.

---

## Phase 2 — Android Action Execution

### Task 2.1: Implement Android action executor
- Add `IActionExecutor` shared interface.
- Register `AndroidActionExecutor` only on Android.
- Validate action type against an allowlist before execution.

### Task 2.2: Open app
- Resolve app label to package name using Android package manager APIs.
- Launch the app if found.
- Fallback to Play Store search or user-visible error.

### Task 2.3: Open URL
- Use Android intent dispatch with `ACTION_VIEW`.

### Task 2.4: Navigation actions
- Implement via Android `AccessibilityService.performGlobalAction()`.
- Support:
  - back
  - home
  - recents
  - notifications

### Task 2.5: Scroll actions
- Use `dispatchGesture()` from the accessibility service.
- Support basic directional scroll gestures.

### Task 2.6: Media actions
- Dispatch media key events for play/pause/next/previous.

### Deliverable
- The assistant can return structured actions and the Android device executes them.

---

## Phase 3 — Assistant Repo Integration

This work belongs in the sibling repo, but the MAUI plan depends on it.

### Assistant repo location
- `c:\Users\MPhil\source\repos\personal-assistant`

### Required endpoint behavior
- `POST /api/command`
- Validate `deviceToken`
- Return `textResponse` plus `actions`

### Current plan assumptions
- Minimal endpoint support already exists or is in progress.
- The MAUI app should treat the assistant as an external dependency and not hardcode local-only behavior.

### Local test workflow
1. Run the assistant from the sibling repo.
2. Confirm the endpoint responds on the local network.
3. Point the MAUI Android app at that base URL.
4. Test command round-trip from device to assistant and back.

---

## Security Requirements

- Require device token authentication for every request.
- Only execute allowlisted action types.
- Do not log device tokens.
- Avoid logging full payloads in release builds.
- Use HTTP only for local/LAN MVP testing.
- Require HTTPS before exposing beyond local trusted environments.

---

## Testing Plan

### TimeCheck app testing
1. Build and deploy the MAUI app to Android.
2. Save assistant URL and token in the app.
3. Trigger speech capture from the UI, then from the notification, then from the Quick Settings tile.
4. Verify the transcribed text reaches `/api/command`.
5. Verify `device.open_app` launches an app.
6. Verify `device.open_url` opens a browser URL.
7. Verify `device.navigate` works after enabling accessibility permissions.
8. Verify `device.media` controls active media playback.
9. Verify invalid token responses are handled cleanly.

### Assistant repo testing
1. Send a valid POST request directly to `/api/command`.
2. Send an invalid-token request and verify rejection.
3. Verify unrecognized commands return text without unsafe actions.

---

## What This MVP Does Not Include

- Always-listening wake word.
- Accessibility node inspection for arbitrary UI automation.
- Free-form text entry into other apps.
- End-to-end encrypted command transport beyond HTTPS.
- Offline command queueing.
- Refactoring the rest of `TimeCheck` unless necessary to support the companion flow.
- Replacing Android Voice Access as a general-purpose accessibility tool.

---

## Longer-Term Direction

If the MVP works well, the next practical question is whether this can grow into a more capable Android voice-control layer for:

- dictating into fields on the phone,
- navigating app UI with higher precision,
- and reducing reliance on Android Voice Access for this specific workflow.

That remains speculative at this stage. The hardest gaps are reliability, arbitrary UI targeting, text-field interaction, permission friction, and the broader accessibility expectations that Voice Access already handles. The MVP should therefore be judged on narrower proof points: command capture, assistant round-trip, and execution of a constrained allowlisted action set.

---

## Recommended Build Order

1. Fix Android build reliability in this repo.
2. Add settings, models, and API client in shared MAUI code.
3. Confirm the sibling assistant API works against the agreed payloads.
4. Add Android speech capture entry from inside the app.
5. Add notification and Quick Settings tile triggers.
6. Add Android action execution.
7. Add accessibility-backed navigation and scroll.

---

## Branching Note

Implementation work for this plan should continue on:

- `feature/maui-android-companion`

If assistant-side changes are needed in the sibling repo, create a separate feature branch there as well so cross-repo changes stay isolated.