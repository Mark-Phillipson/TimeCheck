# Build and Run Instructions for TimeCheck

## Current Status

**✅ Windows**: Fully working
**⚠️ Android**: Currently disabled due to NuGet PackageSourceMapping configuration issues

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
adb pair 192.168.0.3:41737

# Build and run for Android
dotnet run -f net9.0-android
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