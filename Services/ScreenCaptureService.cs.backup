using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace AionDesktopAssistant.Services
{
    public class ScreenCaptureService
    {
        public Bitmap CaptureScreen()
        {
            try
            {
                // Get the size of the primary monitor
                var bounds = Screen.PrimaryScreen.Bounds;

                // Create a bitmap to hold the screenshot
                var bitmap = new Bitmap(bounds.Width, bounds.Height);

                // Create a Graphics object from the bitmap
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // Copy the screen to the bitmap
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to capture screen: {ex.Message}", ex);
            }
        }

        public Bitmap CaptureWindow(IntPtr windowHandle)
        {
            try
            {
                // Get window rectangle
                var rect = new RECT();
                GetWindowRect(windowHandle, ref rect);

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                var bitmap = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    IntPtr hdc = graphics.GetHdc();
                    IntPtr windowDC = GetWindowDC(windowHandle);

                    BitBlt(hdc, 0, 0, width, height, windowDC, 0, 0, 0x00CC0020);

                    graphics.ReleaseHdc(hdc);
                    ReleaseDC(windowHandle, windowDC);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to capture window: {ex.Message}", ex);
            }
        }

        public void SaveScreenshot(Bitmap bitmap, string filename)
        {
            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AION Screenshots");
                Directory.CreateDirectory(directory);

                var filePath = Path.Combine(directory, filename);
                bitmap.Save(filePath, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save screenshot: {ex.Message}", ex);
            }
        }

        public byte[] BitmapToByteArray(Bitmap bitmap)
        {
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        // Windows API declarations
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}