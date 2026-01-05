using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using Tesseract;

namespace TapoControllerGUI
{
    public class OCRProcessor : IOCRProcessor
    {
        private TesseractEngine? _engine;
        private readonly string _tessDataPath;
        private bool _isInitialized = false;

        public Action<string>? LogCallback { get; set; }
        public Action<string>? OCRResultCallback { get; set; }

        public OCRProcessor(string tessDataPath = "./tessdata")
        {
            _tessDataPath = tessDataPath;
        }

        public bool Initialize()
        {
            try
            {
                // Get the application's base directory
                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullTessDataPath = Path.Combine(appDir, "tessdata");
                
                // Set the Tesseract DLL path explicitly
                string x64Path = Path.Combine(appDir, "x64");
                string x86Path = Path.Combine(appDir, "x86");
                
                // Try to load from x64 first, then x86, then root
                string dllPath = appDir;
                if (Directory.Exists(x64Path) && File.Exists(Path.Combine(x64Path, "tesseract50.dll")))
                {
                    dllPath = x64Path;
                    LogCallback?.Invoke($"Using x64 Tesseract DLLs from: {x64Path}");
                }
                else if (Directory.Exists(x86Path) && File.Exists(Path.Combine(x86Path, "tesseract50.dll")))
                {
                    dllPath = x86Path;
                    LogCallback?.Invoke($"Using x86 Tesseract DLLs from: {x86Path}");
                }
                else
                {
                    LogCallback?.Invoke($"Using root Tesseract DLLs from: {appDir}");
                }
                
                // Add to PATH
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                Environment.SetEnvironmentVariable("PATH", $"{dllPath};{currentPath}");
                
                // Check if tessdata folder exists in app directory
                if (!Directory.Exists(fullTessDataPath))
                {
                    LogCallback?.Invoke($"Creating tessdata directory at: {fullTessDataPath}");
                    Directory.CreateDirectory(fullTessDataPath);
                    LogCallback?.Invoke("Please download tessdata files from: https://github.com/tesseract-ocr/tessdata");
                    LogCallback?.Invoke("Place 'eng.traineddata' in the tessdata folder for English OCR support.");
                    return false;
                }

                // Check if eng.traineddata exists
                string trainedDataFile = Path.Combine(fullTessDataPath, "eng.traineddata");
                if (!File.Exists(trainedDataFile))
                {
                    LogCallback?.Invoke($"Missing eng.traineddata file at: {trainedDataFile}");
                    LogCallback?.Invoke("Please download from: https://github.com/tesseract-ocr/tessdata/raw/main/eng.traineddata");
                    return false;
                }

                LogCallback?.Invoke($"Using tessdata path: {fullTessDataPath}");
                LogCallback?.Invoke($"Found eng.traineddata: {new FileInfo(trainedDataFile).Length} bytes");
                
                // Check for native DLLs
                string tesseractDll = Path.Combine(appDir, "tesseract50.dll");
                string leptonicaDll = Path.Combine(appDir, "leptonica-1.82.0.dll");
                LogCallback?.Invoke($"tesseract50.dll exists: {File.Exists(tesseractDll)}");
                LogCallback?.Invoke($"leptonica-1.82.0.dll exists: {File.Exists(leptonicaDll)}");

                // Initialize Tesseract engine with English language
                _engine = new TesseractEngine(fullTessDataPath, "eng", EngineMode.Default);
                _isInitialized = true;
                LogCallback?.Invoke("OCR engine initialized successfully.");
                return true;
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"Failed to initialize OCR engine: {ex.Message}");
                if (ex.InnerException != null)
                {
                    LogCallback?.Invoke($"Inner exception: {ex.InnerException.Message}");
                }
                LogCallback?.Invoke("Note: Tesseract requires Visual C++ Redistributable. Install from: https://aka.ms/vs/17/release/vc_redist.x64.exe");
                _isInitialized = false;
                return false;
            }
        }

        public async Task<string> ProcessFrameAsync(Bitmap frame)
        {
            if (!_isInitialized || _engine == null)
            {
                return "OCR not initialized";
            }

            return await Task.Run(() =>
            {
                try
                {
                    // Preprocess image for better OCR results
                    using (var processedFrame = PreprocessImage(frame))
                    {
                        // Convert Bitmap to Pix using the correct API
                        using (var ms = new MemoryStream())
                        {
                            processedFrame.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Position = 0;
                            
                            using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                            using (var page = _engine.Process(pix))
                            {
                                var text = page.GetText();
                                var confidence = page.GetMeanConfidence();
                                
                                // Clean up the text
                                text = text?.Trim() ?? string.Empty;
                                
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    LogCallback?.Invoke($"OCR detected (confidence: {confidence:P0}): {text.Replace("\n", " | ")}");
                                    OCRResultCallback?.Invoke(text);
                                    return text;
                                }
                                
                                return string.Empty;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogCallback?.Invoke($"OCR processing error: {ex.Message}");
                    return $"Error: {ex.Message}";
                }
            });
        }

        private Bitmap PreprocessImage(Bitmap original)
        {
            // Create a copy to work with
            Bitmap processed = new Bitmap(original.Width, original.Height);
            
            using (Graphics g = Graphics.FromImage(processed))
            {
                // Improve image quality for OCR
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                
                g.DrawImage(original, 0, 0, original.Width, original.Height);
            }
            
            // Convert to grayscale for better OCR
            processed = ConvertToGrayscale(processed);
            
            return processed;
        }

        private Bitmap ConvertToGrayscale(Bitmap original)
        {
            Bitmap grayscale = new Bitmap(original.Width, original.Height);
            
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixelColor = original.GetPixel(x, y);
                    int grayScale = (int)((pixelColor.R * 0.3) + (pixelColor.G * 0.59) + (pixelColor.B * 0.11));
                    Color grayColor = Color.FromArgb(pixelColor.A, grayScale, grayScale, grayScale);
                    grayscale.SetPixel(x, y, grayColor);
                }
            }
            
            return grayscale;
        }

        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
            _isInitialized = false;
        }
    }
}
