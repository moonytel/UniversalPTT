# 🎤 Universal Push-to-Talk

A lightweight, universal push-to-talk application for Windows that works with any microphone and supports both keyboard and mouse triggers.

![Screenshot](screenshot.png)

## ✨ Features

- **Universal Compatibility**: Works with any Windows application
- **Flexible Input**: Support for keyboard keys and mouse buttons as PTT triggers
- **Smart Microphone Management**: Automatically unmutes microphone when app closes
- **System Tray Integration**: Runs minimized in the background
- **Visual Feedback**: Real-time status display and notifications
- **Easy Configuration**: Simple one-click setup for PTT triggers
- **Lightweight**: Single executable file, no installation required

## 🚀 Quick Start

1. **Download** the latest release from the [Releases](../../releases) page
2. **Run** `UniversalPTT.exe` - no installation needed!
3. **Click** "Capture New Key/Button" and press your desired PTT key
4. **Start talking** - hold your PTT key to unmute, release to mute

## 📋 System Requirements

- Windows 10 or later (64-bit)
- .NET 6.0 Runtime (automatically included in single-file version)
- Microphone/audio input device

## 🔧 Usage

### Setting Up PTT Trigger
1. Click **"📝 Capture New Key/Button"**
2. Press any keyboard key or mouse button
3. Your selection is automatically saved

### Configuration Options
- **🗕 Start minimized to system tray**: App starts hidden in system tray
- **🔔 Show notifications**: Display popup notifications for status changes
- **🔇 Auto-mute microphone on start**: Automatically mute mic when app starts

### System Tray
- **Double-click** tray icon to show the main window
- **Right-click** for context menu with show/exit options

## 🛠️ Building from Source

### Prerequisites
- Visual Studio 2022 or later
- .NET 6.0 SDK
- Windows 10 SDK

### Build Steps
```bash
# Clone the repository
git clone https://github.com/moonytel/UniversalPTT.git
cd UniversalPTT

# Restore dependencies
dotnet restore

# Build single-file executable
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Output will be in: bin/Release/net6.0-windows/win-x64/publish/
```

### Development Build
```bash
# For development/debugging
dotnet build -c Debug
dotnet run
```

## 📁 Project Structure

```
UniversalPTT/
├── Program.cs              # Main application code
├── UniversalPTT.csproj    # Project file
├── config.json            # User configuration (auto-generated)
├── icon.ico               # Application icon
├── README.md              # This file
├── LICENSE                # MIT License
└── screenshot.png         # Application screenshot
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 Configuration File

The app automatically creates a `config.json` file in the same directory:

```json
{
  "DeviceType": 0,           // 0 = Keyboard, 1 = Mouse
  "HotKey": 20,              // Virtual key code (20 = CapsLock)
  "HotButton": 1048576,      // Mouse button flag
  "StartMinimized": false,   // Start in system tray
  "ShowNotifications": true, // Show popup notifications
  "AutoMuteOnStart": true   // Auto-mute on startup
}
```

## 🔒 Privacy & Security

- **No data collection**: All settings stored locally
- **No network access**: Application works completely offline
- **Open source**: Full source code available for review
- **Minimal permissions**: Only requires microphone access

## 🐛 Troubleshooting

### Common Issues

**Microphone not being controlled:**
- Check Windows audio permissions
- Ensure default microphone is set correctly
- Try running as administrator

**PTT key not working:**
- Check if another application is using the same key
- Try a different key combination
- Restart the application

**Application not starting:**
- Ensure .NET 6.0 is installed (for framework-dependent builds)
- Check Windows Defender/antivirus settings
- Try running as administrator

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [NAudio](https://github.com/naudio/NAudio) for audio management
- Uses Windows Forms for the user interface
- Inspired by various push-to-talk applications

## 📞 Support

If you encounter any issues or have questions:
- Open an [issue](../../issues) on GitHub
- Check existing issues for solutions
- Provide detailed information about your system and the problem

---

**⭐ Star this repository if you find it useful!**