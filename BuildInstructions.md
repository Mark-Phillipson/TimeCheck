# Build and Run Instructions for TimeCheck
## Building the Project

To build the project, open a terminal in the project root directory and run:

```bash
dotnet build TimeCheck.csproj
```

## Running the Project

To run the project, use one of these commands depending on your target platform:
adb devices

### Androidw'hhtime'
```bash
dotnet run -f net9.0-android
dotnet build -f:net9.0-android -c:Debug /t:Install
dotnet build -f net9.0-android && dotnet build -t:Run -f net9.0-android
```

### Windows
```bash
dotnet run -f net9.0-windows10.0.19041.0
```

### iOS (requires Mac)
```bash
dotnet run -f net9.0-ios
```

### Mac Catalyst (requires Mac)
```bash
dotnet run -f net9.0-maccatalyst
```

## Note
- Make sure you have the .NET MAUI workload installed
- For Android deployment, ensure you have Android SDK installed and an emulator or device ready
- For iOS/Mac Catalyst, you need a Mac with Xcode installed