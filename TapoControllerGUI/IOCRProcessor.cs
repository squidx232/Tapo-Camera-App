using System;
using System.Drawing;
using System.Threading.Tasks;

namespace TapoControllerGUI
{
    public interface IOCRProcessor : IDisposable
    {
        Action<string>? LogCallback { get; set; }
        Action<string>? OCRResultCallback { get; set; }
        bool Initialize();
        Task<string> ProcessFrameAsync(Bitmap frame);
    }
}
