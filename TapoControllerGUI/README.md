# Tapo Camera Controller GUI

A Windows Forms application for managing and controlling TP-Link Tapo PTZ cameras on your local network.

## Features

### 🎥 Camera Management
- **Network Scanning**: Automatically discover Tapo cameras on your local network
- **Multi-Camera Support**: Manage multiple cameras simultaneously
- **Camera Simulator**: Built-in simulator for testing without physical cameras

### 🎮 PTZ Controls
- **Pan/Tilt Control**: Move camera in all directions (Up, Down, Left, Right)
- **Preset Positions**: Quick access to saved camera positions
- **Calibration**: Recalibrate camera position

### 📺 Video Streaming
- **RTSP Streaming**: View live camera feeds using LibVLC
- **Multiple Quality Options**: Support for different stream qualities
- **Snapshot Capture**: Take snapshots from the video feed

### 🔤 OCR Text Recognition (NEW!)
- **Automatic Text Scanning**: Periodically capture and analyze frames for text
- **PLC/HMI Monitoring**: Read data from industrial displays and HMI screens
- **Configurable Intervals**: Set scan frequency from 1-30 seconds
- **Manual Capture**: Instantly capture and analyze current frame
- **Real-time Display**: View detected text with timestamps
- **Visual Feedback**: Green border indicator shows when OCR is actively scanning
- **Windows Built-in OCR**: Uses native Windows 10/11 OCR engine (no additional setup required!)

## Requirements

- .NET 10.0 or later
- Windows OS (Windows Forms application)
- TP-Link Tapo cameras on the same network

## Dependencies

- **Smdn.TPSmartHomeDevices.Tapo** (v2.1.0) - Tapo device communication
- **LibVLCSharp** (v3.9.5) - Video streaming
- **AForge** (v2.2.5) - Video processing
- **VideoLAN.LibVLC.Windows** (v3.0.21) - VLC media player library
- **Windows.Media.Ocr** - Built-in Windows OCR (requires Windows 10+)
- **Tesseract** (v5.2.0) - OCR fallback option

## Building the Project

**IMPORTANT**: Close the application if it's running before building!

```powershell
dotnet build
```

### Running the Application

```powershell
# Use the batch file for easy launching
.\RunTapoWithOCR.bat

# Or run directly
dotnet run
```

### OCR Setup

**Good News!** OCR works out-of-the-box on Windows 10/11 using the built-in Windows.Media.Ocr engine. No additional setup required! 🎉

The application will automatically use Windows OCR for text recognition.

## Running the Application

```powershell
dotnet run
```

Or build and run the executable from Visual Studio.

## Project Structure

- **ProfessionalTapoForm.cs** - Main application form with network scanning and camera discovery
- **PTZControlForm.cs** - PTZ control interface for camera movement
- **VideoViewerForm.cs** - Video streaming viewer using LibVLC
- **TapoPTZCamera.cs** - Camera control logic and API wrapper
- **MultiCameraSimulator.cs** - Camera simulator for testing

## Known Issues

### ⚠️ Authentication Issue

**Status**: The authentication mechanism in `TapoPTZCamera.cs` currently has a known limitation.

**Problem**: The Smdn.TPSmartHomeDevices.Tapo library v2.1.0 requires `ITapoCredentialIdentity` and `ITapoCredentialProvider` objects for authentication, but the public API methods to create these objects are not clearly documented or exposed in the current version.

**Current State**: The `AuthenticateAsync` method passes `null` values, which allows compilation but will fail at runtime when attempting to authenticate with actual cameras.

**Potential Solutions**:
1. **Investigate Library Source**: Check the [Smdn.TPSmartHomeDevices.Tapo GitHub repository](https://github.com/smdn/Smdn.TPSmartHomeDevices) for correct API usage examples
2. **Update Library Version**: Check if a newer version of the library has better documentation or different API
3. **Implement Custom Classes**: Create custom implementations of `ITapoCredentialIdentity` and `ITapoCredentialProvider`
4. **Contact Library Author**: Reach out to the library maintainer for clarification

**Code Location**: `TapoPTZCamera.cs`, line 35-56 (AuthenticateAsync method)

## Usage

1. **Launch Application**: Start the application to see the main control panel
2. **Network Scan**: 
   - Enter your subnet (e.g., `192.168.1`)
   - Click "Scan Network" to discover cameras
3. **Camera Simulator** (for testing):
   - Set the number of simulated cameras
   - Click "Start Simulators"
   - Simulated cameras will appear in the network scan
4. **Control Camera**:
   - Select a camera from the discovered list
   - Click "Control PTZ" to open the control panel
   - Use arrow buttons for pan/tilt control
   - Use preset buttons to move to saved positions
5. **View Stream**:
   - Select a camera
   - Click "Start Stream" and enter credentials
   - Video will appear in the Live Camera Stream panel
6. **Use OCR** (for PLC/HMI monitoring):
   - Ensure video stream is active
   - Check "Enable OCR" to start automatic text scanning
   - Adjust scan interval as needed (default: 2 seconds)
   - Click "Capture Now" for immediate text detection
   - OCR readings appear in the text box below with timestamps

## Configuration

### Network Settings
- Default subnet: Auto-detected from your network adapter
- Scan range: IP addresses 1-254 in the subnet
- Timeout: 2 seconds per IP

### Video Streaming
- Default protocol: RTSP
- Supported formats: H.264, H.265
- Quality options: Low, Medium, High

## Contributing

Contributions are welcome! Key areas for improvement:
- Fix authentication implementation
- Add more camera models support
- Improve error handling
- Add configuration persistence
- Enhance UI/UX

## License

This project is provided as-is for educational and personal use.

## Acknowledgments

- Smdn - For the TPSmartHomeDevices.Tapo library
- VideoLAN - For LibVLC
- AForge.NET - For video processing capabilities
