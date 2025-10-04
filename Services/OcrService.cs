using System;
using System.Drawing;
using System.Threading.Tasks;
using Tesseract;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
using Serilog;
using System.Diagnostics;

namespace AionDesktopAssistant.Services
{
    public class OcrService
    {
        private static readonly ILogger _logger = Log.ForContext<OcrService>();
        private readonly string _tessDataPath;
        private TesseractEngine? _engine;

        public OcrService()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("👁️ Initializing OcrService...");

            try
            {
                // Set up Tesseract data path
                _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                _logger.Debug("📁 Tesseract data path: {TessDataPath}", _tessDataPath);

                InitializeTesseract();

                stopwatch.Stop();
                _logger.Information("✅ OcrService initialized successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Fatal(ex, "❌ Failed to initialize OcrService after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private void InitializeTesseract()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔧 Initializing Tesseract engine...");

            try
            {
                // Create tessdata directory if it doesn't exist
                Directory.CreateDirectory(_tessDataPath);
                _logger.Debug("📁 Created tessdata directory: {Path}", _tessDataPath);

                // Initialize Tesseract engine with English language
                _engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                _logger.Debug("🌐 Tesseract engine created with language: eng");

                _engine.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?@#$%^&*()_+-=[]{}|;':\",./<>? \n\r\t");

                stopwatch.Stop();
                _logger.Information("✅ Tesseract engine initialized successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to initialize Tesseract engine after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to initialize Tesseract: {ex.Message}. Please ensure tessdata is available.", ex);
            }
        }

        public async Task<string> ExtractTextAsync(Bitmap bitmap)
        {
            var stopwatch = Stopwatch.StartNew();
            var imageSize = $"{bitmap.Width}x{bitmap.Height}";
            _logger.Debug("📸 Starting async OCR text extraction for image {ImageSize}", imageSize);

            try
            {
                var result = await Task.Run(() => ExtractText(bitmap));
                stopwatch.Stop();

                _logger.Information("✅ Async OCR completed in {ElapsedMs}ms - Image: {ImageSize}, TextLength: {TextLength}",
                    stopwatch.ElapsedMilliseconds, imageSize, result.Length);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Async OCR failed after {ElapsedMs}ms for image {ImageSize}",
                    stopwatch.ElapsedMilliseconds, imageSize);
                throw;
            }
        }

        public string ExtractText(Bitmap bitmap)
        {
            var stopwatch = Stopwatch.StartNew();
            var imageSize = $"{bitmap.Width}x{bitmap.Height}";
            var imagePixels = bitmap.Width * bitmap.Height;

            _logger.Debug("🔍 Starting OCR text extraction - Image: {ImageSize}, Pixels: {PixelCount}",
                imageSize, imagePixels);

            try
            {
                if (_engine == null)
                {
                    _logger.Error("❌ OCR engine not initialized");
                    throw new InvalidOperationException("OCR engine not initialized");
                }

                // Preprocess the image for better OCR results
                var preprocessStart = Stopwatch.StartNew();
                var processedBitmap = PreprocessImage(bitmap);
                preprocessStart.Stop();

                _logger.Debug("🔧 Image preprocessing completed in {PreprocessMs}ms", preprocessStart.ElapsedMilliseconds);

                // Convert bitmap to Tesseract format and process
                var ocrStart = Stopwatch.StartNew();
                using (var pix = PixConverter.ToPix(processedBitmap))
                {
                    using (var page = _engine.Process(pix))
                    {
                        var text = page.GetText();
                        var confidence = page.GetMeanConfidence();
                        ocrStart.Stop();

                        _logger.Debug("📊 OCR processing completed - Confidence: {Confidence:P1}, ProcessingTime: {OcrMs}ms",
                            confidence, ocrStart.ElapsedMilliseconds);

                        // Only return text if confidence is reasonable
                        if (confidence > 0.3f)
                        {
                            var result = text?.Trim() ?? string.Empty;
                            stopwatch.Stop();

                            _logger.Information("✅ OCR text extraction successful - Image: {ImageSize}, Confidence: {Confidence:P1}, TextLength: {TextLength}, TotalTime: {TotalMs}ms",
                                imageSize, confidence, result.Length, stopwatch.ElapsedMilliseconds);

                            if (!string.IsNullOrEmpty(result))
                            {
                                _logger.Debug("📝 Extracted text preview: {TextPreview}",
                                    result.Length > 100 ? result[..100] + "..." : result);
                            }

                            return result;
                        }
                        else
                        {
                            stopwatch.Stop();
                            _logger.Warning("⚠️ OCR confidence too low ({Confidence:P1}) - returning empty string. Image: {ImageSize}, Time: {TotalMs}ms",
                                confidence, imageSize, stopwatch.ElapsedMilliseconds);
                            return string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ OCR processing failed after {ElapsedMs}ms - Image: {ImageSize}",
                    stopwatch.ElapsedMilliseconds, imageSize);
                throw new InvalidOperationException($"OCR processing failed: {ex.Message}", ex);
            }
        }

        private Bitmap PreprocessImage(Bitmap originalBitmap)
        {
            var stopwatch = Stopwatch.StartNew();
            var imageSize = $"{originalBitmap.Width}x{originalBitmap.Height}";

            _logger.Debug("🔧 Starting image preprocessing - Size: {ImageSize}", imageSize);

            try
            {
                // Convert Bitmap to OpenCV Mat
                using (var mat = BitmapConverter.ToMat(originalBitmap))
                {
                    _logger.Debug("🔄 Converted bitmap to OpenCV Mat");

                    // Convert to grayscale
                    using (var gray = new Mat())
                    {
                        Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);
                        _logger.Debug("🎨 Converted to grayscale");

                        // Apply Gaussian blur to reduce noise
                        using (var blurred = new Mat())
                        {
                            Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);
                            _logger.Debug("🌊 Applied Gaussian blur");

                            // Apply adaptive thresholding
                            using (var thresh = new Mat())
                            {
                                Cv2.AdaptiveThreshold(blurred, thresh, 255, AdaptiveThresholdTypes.GaussianC,
                                    ThresholdTypes.Binary, 11, 2);
                                _logger.Debug("📊 Applied adaptive thresholding");

                                // Apply morphological operations to clean up the image
                                using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2)))
                                {
                                    using (var cleaned = new Mat())
                                    {
                                        Cv2.MorphologyEx(thresh, cleaned, MorphTypes.Close, kernel);
                                        _logger.Debug("🧹 Applied morphological operations");

                                        // Convert back to Bitmap
                                        var result = BitmapConverter.ToBitmap(cleaned);
                                        stopwatch.Stop();

                                        _logger.Debug("✅ Image preprocessing completed successfully in {ElapsedMs}ms - Size: {ImageSize}",
                                            stopwatch.ElapsedMilliseconds, imageSize);

                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Warning(ex, "⚠️ Image preprocessing failed after {ElapsedMs}ms - using original bitmap. Size: {ImageSize}",
                    stopwatch.ElapsedMilliseconds, imageSize);
                return originalBitmap;
            }
        }

        public async Task<string> ExtractTextFromRegionAsync(Bitmap bitmap, Rectangle region)
        {
            var stopwatch = Stopwatch.StartNew();
            var imageSize = $"{bitmap.Width}x{bitmap.Height}";
            var regionInfo = $"{region.Width}x{region.Height} at ({region.X},{region.Y})";

            _logger.Debug("✂️ Starting regional OCR - Image: {ImageSize}, Region: {RegionInfo}", imageSize, regionInfo);

            return await Task.Run(() =>
            {
                try
                {
                    // Crop the bitmap to the specified region
                    using (var croppedBitmap = new Bitmap(region.Width, region.Height))
                    {
                        using (var graphics = Graphics.FromImage(croppedBitmap))
                        {
                            graphics.DrawImage(bitmap, new Rectangle(0, 0, region.Width, region.Height), region, GraphicsUnit.Pixel);
                        }

                        _logger.Debug("✂️ Image cropped to region: {RegionInfo}", regionInfo);

                        var result = ExtractText(croppedBitmap);
                        stopwatch.Stop();

                        _logger.Information("✅ Regional OCR completed in {ElapsedMs}ms - Region: {RegionInfo}, TextLength: {TextLength}",
                            stopwatch.ElapsedMilliseconds, regionInfo, result.Length);

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.Error(ex, "❌ Regional OCR failed after {ElapsedMs}ms - Region: {RegionInfo}",
                        stopwatch.ElapsedMilliseconds, regionInfo);
                    throw new InvalidOperationException($"Failed to extract text from region: {ex.Message}", ex);
                }
            });
        }

        public async Task<string> ExtractTextFromFileAsync(string imagePath)
        {
            var stopwatch = Stopwatch.StartNew();
            var fileName = Path.GetFileName(imagePath);

            _logger.Debug("📁 Starting file OCR - File: {FileName}, Path: {FilePath}", fileName, imagePath);

            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(imagePath))
                    {
                        _logger.Error("📁 File not found: {FilePath}", imagePath);
                        throw new FileNotFoundException($"Image file not found: {imagePath}");
                    }

                    var fileInfo = new FileInfo(imagePath);
                    _logger.Debug("📊 File info - Size: {FileSize} bytes, LastModified: {LastModified}",
                        fileInfo.Length, fileInfo.LastWriteTime);

                    using (var bitmap = new Bitmap(imagePath))
                    {
                        var imageSize = $"{bitmap.Width}x{bitmap.Height}";
                        _logger.Debug("🖼️ Loaded image from file - Size: {ImageSize}", imageSize);

                        var result = ExtractText(bitmap);
                        stopwatch.Stop();

                        _logger.Information("✅ File OCR completed in {ElapsedMs}ms - File: {FileName}, Size: {ImageSize}, TextLength: {TextLength}",
                            stopwatch.ElapsedMilliseconds, fileName, imageSize, result.Length);

                        return result;
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.Error(ex, "❌ File OCR failed after {ElapsedMs}ms - File: {FileName}",
                        stopwatch.ElapsedMilliseconds, fileName);
                    throw new InvalidOperationException($"Failed to extract text from file: {ex.Message}", ex);
                }
            });
        }

        public void Dispose()
        {
            _logger.Information("🗑️ Disposing OcrService...");

            try
            {
                _engine?.Dispose();
                _logger.Information("✅ OcrService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "❌ Error disposing OcrService");
            }
        }
    }
}