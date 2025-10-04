using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using Serilog;
using System.Diagnostics;

namespace AionDesktopAssistant.Services
{
    public class ScreenCaptureService
    {
        private static readonly ILogger _logger = Log.ForContext<ScreenCaptureService>();

        public ScreenCaptureService()
        {
            _logger.Information("📸 ScreenCaptureService initialized");
        }

        public Bitmap CaptureScreen()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🖥️ Starting full screen capture...");

            try
            {
                // Get the size of the primary monitor
                var bounds = Screen.PrimaryScreen.Bounds;
                var screenSize = $"{bounds.Width}x{bounds.Height}";

                _logger.Debug("📐 Primary screen bounds: {ScreenSize} at ({X},{Y})",
                    screenSize, bounds.X, bounds.Y);

                // Create a bitmap to hold the screenshot
                var bitmap = new Bitmap(bounds.Width, bounds.Height);
                _logger.Debug("💾 Created bitmap for screen capture: {ScreenSize}", screenSize);

                // Create a Graphics object from the bitmap
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // Copy the screen to the bitmap
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
                    _logger.Debug("🎯 Copied screen content to bitmap");
                }

                stopwatch.Stop();
                var fileSizeKB = (bounds.Width * bounds.Height * 4) / 1024; // Rough estimate

                _logger.Information("✅ Screen capture completed in {ElapsedMs}ms - Size: {ScreenSize}, EstimatedSize: {FileSizeKB}KB",
                    stopwatch.ElapsedMilliseconds, screenSize, fileSizeKB);

                return bitmap;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Screen capture failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to capture screen: {ex.Message}", ex);
            }
        }

        public Bitmap CaptureWindow(IntPtr windowHandle)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🪟 Starting window capture - Handle: {WindowHandle:X}", windowHandle.ToInt64());

            try
            {
                // Get window rectangle
                var rect = new RECT();
                bool rectResult = GetWindowRect(windowHandle, ref rect);

                if (!rectResult)
                {
                    _logger.Warning("⚠️ Failed to get window rectangle for handle {WindowHandle:X}", windowHandle.ToInt64());
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                var windowSize = $"{width}x{height}";

                _logger.Debug("📐 Window dimensions: {WindowSize} at ({Left},{Top})",
                    windowSize, rect.Left, rect.Top);

                if (width <= 0 || height <= 0)
                {
                    _logger.Warning("⚠️ Invalid window dimensions: {WindowSize}", windowSize);
                    throw new InvalidOperationException($"Invalid window dimensions: {windowSize}");
                }

                var bitmap = new Bitmap(width, height);
                _logger.Debug("💾 Created bitmap for window capture: {WindowSize}", windowSize);

                using (var graphics = Graphics.FromImage(bitmap))
                {
                    IntPtr hdc = graphics.GetHdc();
                    IntPtr windowDC = GetWindowDC(windowHandle);

                    if (windowDC == IntPtr.Zero)
                    {
                        _logger.Warning("⚠️ Failed to get window device context");
                    }

                    bool bitBltResult = BitBlt(hdc, 0, 0, width, height, windowDC, 0, 0, 0x00CC0020);

                    if (!bitBltResult)
                    {
                        _logger.Warning("⚠️ BitBlt operation failed for window capture");
                    }
                    else
                    {
                        _logger.Debug("🎯 Successfully copied window content to bitmap");
                    }

                    graphics.ReleaseHdc(hdc);
                    ReleaseDC(windowHandle, windowDC);
                }

                stopwatch.Stop();
                var fileSizeKB = (width * height * 4) / 1024; // Rough estimate

                _logger.Information("✅ Window capture completed in {ElapsedMs}ms - Handle: {WindowHandle:X}, Size: {WindowSize}, EstimatedSize: {FileSizeKB}KB",
                    stopwatch.ElapsedMilliseconds, windowHandle.ToInt64(), windowSize, fileSizeKB);

                return bitmap;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Window capture failed after {ElapsedMs}ms - Handle: {WindowHandle:X}",
                    stopwatch.ElapsedMilliseconds, windowHandle.ToInt64());
                throw new InvalidOperationException($"Failed to capture window: {ex.Message}", ex);
            }
        }

        public void SaveScreenshot(Bitmap bitmap, string filename)
        {
            var stopwatch = Stopwatch.StartNew();
            var imageSize = $"{bitmap.Width}x{bitmap.Height}";

            _logger.Debug("💾 Starting screenshot save - File: {FileName}, Size: {ImageSize}",
                filename, imageSize);

            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AION Screenshots");

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.Debug("📁 Created screenshots directory: {Directory}", directory);
                }

                var filePath = Path.Combine(directory, filename);
                _logger.Debug("📄 Full file path: {FilePath}", filePath);

                bitmap.Save(filePath, ImageFormat.Png);

                var fileInfo = new FileInfo(filePath);
                stopwatch.Stop();

                _logger.Information("✅ Screenshot saved successfully in {ElapsedMs}ms - File: {FileName}, Size: {ImageSize}, FileSize: {FileSizeKB}KB",
                    stopwatch.ElapsedMilliseconds, filename, imageSize, fileInfo.Length / 1024);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Screenshot save failed after {ElapsedMs}ms - File: {FileName}",
                    stopwatch.ElapsedMilliseconds, filename);
                throw new InvalidOperationException($"Failed to save screenshot: {ex.Message}", ex);
            }
        }

        public byte[] BitmapToByteArray(Bitmap bitmap)
        {
            var stopwatch = Stopwatch.StartNew();
            var imageSize = $"{bitmap.Width}x{bitmap.Height}";

            _logger.Debug("🔄 Converting bitmap to byte array - Size: {ImageSize}", imageSize);

            try
            {
                using (var stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    var result = stream.ToArray();

                    stopwatch.Stop();

                    _logger.Information("✅ Bitmap conversion completed in {ElapsedMs}ms - Size: {ImageSize}, ByteArraySize: {ByteArraySizeKB}KB",
                        stopwatch.ElapsedMilliseconds, imageSize, result.Length / 1024);

                    return result;
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Bitmap conversion failed after {ElapsedMs}ms - Size: {ImageSize}",
                    stopwatch.ElapsedMilliseconds, imageSize);
                throw;
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