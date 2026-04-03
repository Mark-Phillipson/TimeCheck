## Local Device Commands

The **Speak Local Command** button (inside the Companion Assistant panel) opens the microphone and recognises speech without sending anything to the assistant API.  The **Speak Companion Command** button does the same but forwards the recognised text to the configured Personal Assistant API, which maps it to one or more device actions listed below.

### Supported action types

| Action type | Required parameters | Example phrases |
|---|---|---|
| `device.open_app` | `name` | *"open YouTube"*, *"open Spotify"*, *"launch WhatsApp"* |
| `device.open_url` | `url` | *"open bbc.co.uk"*, *"go to https://google.com"* |
| `device.navigate` | `action` | *"go back"*, *"go home"*, *"show recent apps"*, *"show notifications"* |
| `device.scroll` | `direction` | *"scroll up"*, *"scroll down"*, *"scroll left"*, *"scroll right"* |
| `device.media` | `action` | *"play"*, *"pause"*, *"next track"*, *"previous track"* |

### `device.open_app` — recognised app aliases

| Say… | App launched |
|---|---|
| *"open Chrome"* or *"open Google Chrome"* | Google Chrome |
| *"open YouTube"* | YouTube |
| *"open Maps"* or *"open Google Maps"* | Google Maps |
| *"open Gmail"* | Gmail |
| *"open Photos"* or *"open Google Photos"* | Google Photos |
| *"open Settings"* | Android Settings |
| *"open Camera"* | Camera |
| *"open Messages"* | Google Messages |
| *"open Phone"* | Phone / Dialler |
| *"open Spotify"* | Spotify |
| *"open Netflix"* | Netflix |
| *"open Amazon Music"* | Amazon Music |
| *"open WhatsApp"* | WhatsApp |

Any other app name is matched fuzzily against installed application labels; if still not found, the Play Store is opened with a search for the name.

### `device.navigate` — example phrases

| Say… | Action |
|---|---|
| *"go back"* | Back |
| *"go home"* | Home |
| *"show recent apps"* or *"recents"* | Recent Apps |
| *"show notifications"* or *"open notifications"* | Notification shade |

### `device.scroll` — example phrases

| Say… | Action |
|---|---|
| *"scroll up"* | Swipe up |
| *"scroll down"* | Swipe down |
| *"scroll left"* | Swipe left |
| *"scroll right"* | Swipe right |

### `device.media` — example phrases

| Say… | Action |
|---|---|
| *"play"* | Play / Resume |
| *"pause"* | Pause |
| *"next track"* or *"next"* | Skip forward |
| *"previous track"* or *"previous"* | Skip back |

> **Note:** `device.navigate` and `device.scroll` require the **TimeCheck Companion** accessibility service to be active.  Enable it in *Android Settings → Accessibility → TimeCheck Companion*.

---

## Running the application

To run the application using windows:

```sh
cd C:\Users\MPhil\source\repos\TimeCheck\TimeCheck\TimeCheck
dotnet run --framework net10.0-windows10.0.19041.0
    
```

To run the application on Android:

```sh
cd C:\Users\MPhil\source\repos\TimeCheck\TimeCheck\TimeCheck
dotnet build -t:Run -f net10.0-android
```

dotnet build -t:Run -f net10.0-android