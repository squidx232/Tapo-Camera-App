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
                // Preprocess image for better OCR
                using (var processedFrame = PreprocessForOCR(frame))
                {
                    // Convert Bitmap to SoftwareBitmap for Windows OCR
                    using (var ms = new MemoryStream())
                    {
                        processedFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
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
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"OCR processing error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
        
        private Bitmap PreprocessForOCR(Bitmap original)
        {
            // Scale up the image 2x for better OCR accuracy
            int newWidth = original.Width * 2;
            int newHeight = original.Height * 2;
            
            Bitmap enlarged = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(enlarged))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            
            // Convert to grayscale
            Bitmap grayscale = new Bitmap(newWidth, newHeight);
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    Color pixelColor = enlarged.GetPixel(x, y);
                    int grayValue = (int)(pixelColor.R * 0.299 + pixelColor.G * 0.587 + pixelColor.B * 0.114);
                    
                    // Apply contrast enhancement
                    grayValue = (int)((grayValue - 128) * 1.5 + 128);
                    grayValue = Math.Max(0, Math.Min(255, grayValue));
                    
                    Color grayColor = Color.FromArgb(grayValue, grayValue, grayValue);
                    grayscale.SetPixel(x, y, grayColor);
                }
            }
            
            enlarged.Dispose();
            
            // Apply simple sharpening
            Bitmap sharpened = ApplySharpening(grayscale);
            grayscale.Dispose();
            
            return sharpened;
        }
        
        private Bitmap ApplySharpening(Bitmap image)
        {
            Bitmap result = new Bitmap(image.Width, image.Height);
            
            // Simple sharpening kernel
            double[,] kernel = {
                {  0, -1,  0 },
                { -1,  5, -1 },
                {  0, -1,  0 }
            };
            
            for (int y = 1; y < image.Height - 1; y++)
            {
                for (int x = 1; x < image.Width - 1; x++)
                {
                    double sum = 0;
                    
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            Color pixel = image.GetPixel(x + kx, y + ky);
                            sum += pixel.R * kernel[ky + 1, kx + 1];
                        }
                    }
                    
                    int value = (int)Math.Max(0, Math.Min(255, sum));
                    result.SetPixel(x, y, Color.FromArgb(value, value, value));
                }
            }
            
            // Copy edges
            for (int x = 0; x < image.Width; x++)
            {
                result.SetPixel(x, 0, image.GetPixel(x, 0));
                result.SetPixel(x, image.Height - 1, image.GetPixel(x, image.Height - 1));
            }
            for (int y = 0; y < image.Height; y++)
            {
                result.SetPixel(0, y, image.GetPixel(0, y));
                result.SetPixel(image.Width - 1, y, image.GetPixel(image.Width - 1, y));
            }
            
            return result;
        }

        public void Dispose()
        {
            _ocrEngine = null;
            _isInitialized = false;
        }
    }
}
