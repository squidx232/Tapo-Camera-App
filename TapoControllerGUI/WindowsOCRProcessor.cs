using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace TapoControllerGUI
{
    public class WindowsOCRProcessor : IOCRProcessor
    {
        private OcrEngine? _ocrEngine;
        private bool _isInitialized = false;

        public Action<string>? LogCallback { get; set; }
        public Action<string>? OCRResultCallback { get; set; }

        public bool Initialize()
        {
            try
            {
                LogCallback?.Invoke("Attempting to initialize Windows OCR engine...");
                
                // Use Windows built-in OCR engine (requires Windows 10+)
                var language = new Language("en-US");
                LogCallback?.Invoke($"Created language: {language.DisplayName}");
                
                _ocrEngine = OcrEngine.TryCreateFromLanguage(language);
                
                if (_ocrEngine == null)
                {
                    LogCallback?.Invoke("Failed to create Windows OCR engine. Ensure Windows 10+ is installed.");
                    return false;
                }

                _isInitialized = true;
                LogCallback?.Invoke("Windows OCR engine initialized successfully!");
                return true;
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"Failed to initialize Windows OCR engine: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogCallback?.Invoke($"Inner exception: {ex.InnerException.Message}");
                }
                LogCallback?.Invoke($"Stack trace: {ex.StackTrace}");
                _isInitialized = false;
                return false;
            }
        }

        public async Task<string> ProcessFrameAsync(Bitmap frame)
        {
            if (!_isInitialized || _ocrEngine == null)
            {
                return "OCR not initialized";
            }

            try
            {
                // Convert Bitmap to SoftwareBitmap for Windows OCR
                using (var ms = new MemoryStream())
                {
                    frame.Save(ms, ImageFormat.Bmp);
                    ms.Position = 0;

                    var decoder = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());
                    var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                    // Perform OCR
                    var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);
                    
                    var text = ocrResult.Text?.Trim() ?? string.Empty;
                    
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        LogCallback?.Invoke($"Windows OCR detected: {text.Replace("\n", " | ")}");
                        OCRResultCallback?.Invoke(text);
                        return text;
                    }
                    
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"OCR processing error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }

        public void Dispose()
        {
            _ocrEngine = null;
            _isInitialized = false;
        }
    }
}
