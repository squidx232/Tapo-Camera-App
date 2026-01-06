using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

namespace TapoControllerGUI
{
    public class CloudOCRProcessor : IOCRProcessor
    {
        private readonly HttpClient _httpClient;
        private const string OCR_API_URL = "https://api.ocr.space/parse/image";
        private const string API_KEY = "K87899142388957"; // Free public API key
        
        public Action<string>? LogCallback { get; set; }
        public Action<string>? OCRResultCallback { get; set; }

        public CloudOCRProcessor()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public bool Initialize()
        {
            LogCallback?.Invoke("Cloud OCR processor initialized (OCR.space API)");
            return true;
        }

        public async Task<string> ProcessFrameAsync(Bitmap frame)
        {
            try
            {
                // Convert bitmap to base64
                string base64Image;
                using (var ms = new MemoryStream())
                {
                    frame.Save(ms, ImageFormat.Png);
                    byte[] imageBytes = ms.ToArray();
                    base64Image = Convert.ToBase64String(imageBytes);
                }

                // Prepare the request
                var content = new MultipartFormDataContent();
                content.Add(new StringContent(API_KEY), "apikey");
                content.Add(new StringContent("2"), "OCREngine"); // Engine 2 is best for general text
                content.Add(new StringContent("true"), "detectOrientation");
                content.Add(new StringContent("true"), "scale");
                content.Add(new StringContent($"data:image/png;base64,{base64Image}"), "base64Image");

                // Send request
                var response = await _httpClient.PostAsync(OCR_API_URL, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Parse response
                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    var root = doc.RootElement;
                    
                    if (root.TryGetProperty("ParsedResults", out var results) && results.GetArrayLength() > 0)
                    {
                        var firstResult = results[0];
                        if (firstResult.TryGetProperty("ParsedText", out var parsedText))
                        {
                            var text = parsedText.GetString()?.Trim() ?? string.Empty;
                            
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                LogCallback?.Invoke($"Cloud OCR detected: {text.Replace("\n", " | ")}");
                                OCRResultCallback?.Invoke(text);
                                return text;
                            }
                        }
                    }
                    
                    // Check for errors
                    if (root.TryGetProperty("ErrorMessage", out var errorMsg))
                    {
                        var error = errorMsg.GetString();
                        if (!string.IsNullOrEmpty(error) && error != "null")
                        {
                            LogCallback?.Invoke($"OCR API error: {error}");
                        }
                    }
                }
                
                return string.Empty;
            }
            catch (Exception ex)
            {
                LogCallback?.Invoke($"Cloud OCR error: {ex.Message}");
                return string.Empty;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
