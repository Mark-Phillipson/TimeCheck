# Build and Run Instructions for TimeCheck

## Current Status

**✅ Windows**: Fully working with new dual-mode functionality and 100 cycling encouragement messages
**⚠️ Android**: Currently disabled due to NuGet PackageSourceMapping configuration issues

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
   - Cycling: Every 10 minutes (announces once with random message)

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

### Android (Currently Disabled)

The Android target framework has been temporarily disabled due to NuGet PackageSourceMapping configuration issues that prevent proper package resolution for `Microsoft.NET.ILLink.Tasks`.

**To re-enable Android development:**

1. **Option 1 (Recommended)**: Fix the global PackageSourceMapping configuration
   - This is likely configured in your global NuGet.config or through Visual Studio settings
   - You may need to update your .NET MAUI workloads using Visual Studio Installer instead of command line

2. **Option 2**: Edit `TimeCheck.csproj` to include Android again:
   ```xml
   <TargetFrameworks>net9.0-android;net9.0-windows10.0.19041.0</TargetFrameworks>
   ```

**Previous Android commands (for when it's working):**
```bash
# Check connected devices
adb devices
adb pair 192.168.0.3:33451

# Build and run for Android
dotnet run -f net9.0-android
dotnet run -f net9.0-android --project .\TimeCheck\TimeCheck\TimeCheck.csproj
dotnet build -f:net9.0-android -c:Debug /t:Install
dotnet build -f net9.0-android && dotnet build -t:Run -f net9.0-android
```

## Note

- Make sure you have the .NET MAUI workload installed
- ✅ **Windows**: Currently working and ready for development
- ⚠️ **Android**: Temporarily disabled - requires fixing NuGet PackageSourceMapping issues
- ❌ **iOS/Mac Catalyst**: Removed from project (not needed per requirements)

## Summary

We've successfully resolved the macOS/iOS warning messages by:
1. ✅ Removed iOS and macOS Catalyst target frameworks from the project
2. ✅ Removed unnecessary platform-specific configurations
3. ✅ Got Windows build working
4. ⚠️ Temporarily disabled Android due to NuGet configuration conflicts

Your project now builds and runs successfully on Windows without any macOS-related warnings!