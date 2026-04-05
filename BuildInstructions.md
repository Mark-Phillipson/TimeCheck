# Build and Run Instructions for TimeCheck

## Current Status

**✅ Windows**: Working on `net9.0-windows10.0.19041.0` with new dual-mode functionality and 100 cycling encouragement messages
**✅ Android**: Build, install, and launch confirmed on device using adb and the generated launcher activity

## ✅ Successfully Implemented Features

### Dual-Mode Operation
The TimeCheck app now supports two distinct modes:

1. **Time Check Mode** (Default):
   - Announces the current time every 5 minutes (3 times each announcement)
   - Button text: "Speak Time"
   - Visual indicator: Green button highlight

2. **Cycling Encouragement Mode**:
   - Announces random motivational messages for bicycle riding (especially uphill) every 10 minutes (once per announcement)
   - Button text: "Speak Encouragement"  
   - Visual indicator: Light blue button highlight
   - **100 unique encouraging messages** organized in 10 categories

### User Interface Enhancements
- **Mode Toggle Buttons**: Two buttons at the top to switch between modes
- **Current Mode Indicator**: Label showing which mode is active and its behavior
- **Dynamic Button Text**: Main action button changes text based on current mode
- **Modern Grid Layout**: Replaced obsolete StackLayout with Grid for better performance

### Key Features
- ✅ Always starts in Time Check mode (as requested)
- ✅ Toggle between modes at any time using the interface buttons
- ✅ Automatic announcements based on selected mode
- ✅ Manual trigger button that works for both modes
- ✅ 100 diverse cycling encouragement messages (no more repetition!)
- ✅ Landscape mode compatibility (hides mode controls in landscape)

## ⚡ Quick Start (Windows)

```bash
# Navigate to project directory
cd "c:\Users\MPhil\source\repos\TimeCheck\TimeCheck\TimeCheck"

# Build and run
dotnet build TimeCheck.csproj
dotnet run -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0 --project .\TimeCheck\TimeCheck\TimeCheck.csproj
```

## 🚴‍♂️ How to Use Your New Features

1. **Launch the app** - It starts in Time Check mode by default
2. **Switch modes** - Use the toggle buttons at the top
   - Green button = Time Check Mode (active)
   - Gray/Blue button = Cycling Encouragement Mode
3. **Manual testing** - Press the main button to immediately hear time or encouragement
4. **Automatic announcements**:
   - Time Check: Every 5 minutes (announces 3 times)
   - Cycling: Every 1-10 minutes random (announces once with random message)

## Building the Project


To build the project, open a terminal in the project root directory and run:

```bash
dotnet build TimeCheck.csproj
```

## Running the Project

### Windows (Working)
```bash
dotnet run -f net9.0-windows10.0.19041.0
```

### Android (Working)

Use this verified four-command sequence from `C:\Users\MPhil\source\repos\TimeCheck\TimeCheck\TimeCheck`:

```powershell

adb devices
cd c:\Users\MPhil\source\repos\TimeCheck\TimeCheck\TimeCheck

dotnet build -f net10.0-android -c Debug /t:Install /p:DeviceId=R3CW40BQS0M

adb -s R3CW40BQS0M shell am start -n com.companyname.timecheck/crc64a0fd38e9f8dc419b.MainActivity


```

Notes:
- The `XA1024` warning about `NuGet.config` is benign for this Android build.
- `dotnet run -f net10.0-android --device ...` does not work in this setup because the Android runner does not accept `--device` here.
- The launcher activity is the generated Android name `crc64a0fd38e9f8dc419b.MainActivity`, not `com.companyname.timecheck.MainActivity`.
- If `adb devices` shows more than one device, keep using `-s R3CW40BQS0M` consistently (USB Debugging, authorized device).

## Note

- Make sure you have the .NET MAUI workload installed
- ✅ **Windows**: Currently working and ready for development
- ✅ **Android**: Build, install, and launch verified on device
- ❌ **iOS/Mac Catalyst**: Removed from project (not needed per requirements)

Windows note:
- The Windows target currently runs on `net9.0-windows10.0.19041.0`.
- The previous `net10.0-windows10.0.19041.0` target built but crashed on startup in `Microsoft.UI.Xaml.dll` on this machine.

## Summary

We've successfully resolved the macOS/iOS warning messages by:
1. ✅ Removed iOS and macOS Catalyst target frameworks from the project
2. ✅ Removed unnecessary platform-specific configurations
3. ✅ Got Windows running by switching the Windows target to .NET 9
4. ✅ Verified Android build, install, and launch on device

Your project now builds and runs successfully on Windows and Android without any macOS-related warnings.