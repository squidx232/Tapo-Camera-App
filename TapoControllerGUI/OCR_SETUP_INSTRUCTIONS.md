# OCR Setup Instructions

Your Tapo Camera Controller now includes OCR (Optical Character Recognition) functionality to read text from your camera's video feed, perfect for monitoring PLC HMI displays!

## Required Setup: Tesseract Language Data

Before using OCR features, you need to download the Tesseract trained data files:

### Quick Setup Steps:

1. **Create tessdata folder** (if not exists):
   ```
   mkdir tessdata
   ```

2. **Download English language data**:
   - Go to: https://github.com/tesseract-ocr/tessdata
   - Download `eng.traineddata` (click the file, then click "Download" button)
   - Place it in the `tessdata` folder in your application directory

3. **Optional: Add more languages**:
   - For better number recognition, also download: `eng.traineddata` from tessdata_best repository
   - Other languages available at: https://github.com/tesseract-ocr/tessdata

### Directory Structure:
```
TapoControllerGUI/
├── bin/
│   └── Debug/
│       └── net10.0-windows/
│           └── tessdata/
│               └── eng.traineddata
├── ProfessionalTapoForm.cs
├── OCRProcessor.cs
└── ...
```

## How to Use OCR Feature

### In the Application:

1. **Start Video Stream**:
   - Select a camera from the network scan
   - Click "Start Stream" and enter credentials

2. **OCR Controls** (in the video stream panel):
   - **Enable OCR**: Check this box to start automatic text scanning
   - **Interval (sec)**: Set how often to scan for text (1-30 seconds)
   - **Capture Now**: Manually capture and analyze the current frame

3. **View Results**:
   - OCR readings appear in the text box below the controls
   - Each reading is timestamped
   - Results show confidence levels in the activity log

### Tips for Best OCR Results:

1. **Camera Position**: Position your camera to have a clear view of the HMI text
2. **Focus**: Use PTZ controls to zoom in on text areas
3. **Lighting**: Ensure good lighting on the HMI display
4. **Contrast**: OCR works best with high contrast (dark text on light background)
5. **Scan Interval**: Start with 2-3 seconds, adjust based on how often data changes

### Example Use Cases:

- **PLC Monitoring**: Read temperature, pressure, or other sensor values from HMI
- **Production Tracking**: Monitor production counts or batch numbers
- **Status Monitoring**: Track machine status messages
- **Quality Control**: Read measurement displays

## Troubleshooting

### "OCR processor initialization failed"
- Ensure `tessdata` folder exists in the application directory
- Verify `eng.traineddata` file is present
- Check file permissions

### "OCR readings are inaccurate"
- Improve camera focus and zoom
- Increase lighting on the display
- Try adjusting the camera angle
- Consider cleaning the camera lens

### "No text detected"
- Verify the HMI display is visible in the video stream
- Check if text is large enough (zoom in if needed)
- Ensure text has good contrast with background
- Try the "Capture Now" button to test immediately

## Advanced Configuration

The OCR processor includes image preprocessing:
- Grayscale conversion for better text detection
- High-quality image interpolation
- Automatic contrast enhancement

For custom configurations, modify the `OCRProcessor.cs` file.

## Performance Notes

- OCR processing happens in background threads to avoid UI lag
- Temporary snapshot files are automatically cleaned up
- Memory usage scales with capture frequency and image size
- Recommended interval: 2-5 seconds for continuous monitoring

## Need Help?

Check the Activity Log panel for detailed OCR processing information and error messages.
