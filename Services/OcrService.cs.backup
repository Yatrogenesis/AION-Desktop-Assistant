using System;
using System.Drawing;
using System.Threading.Tasks;
using Tesseract;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;

namespace AionDesktopAssistant.Services
{
    public class OcrService
    {
        private readonly string _tessDataPath;
        private TesseractEngine? _engine;

        public OcrService()
        {
            // Set up Tesseract data path
            _tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            InitializeTesseract();
        }

        private void InitializeTesseract()
        {
            try
            {
                // Create tessdata directory if it doesn't exist
                Directory.CreateDirectory(_tessDataPath);

                // Initialize Tesseract engine with English language
                _engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                _engine.SetVariable("tessedit_char_whitelist", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.,!?@#$%^&*()_+-=[]{}|;':\",./<>? \n\r\t");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize Tesseract: {ex.Message}. Please ensure tessdata is available.", ex);
            }
        }

        public async Task<string> ExtractTextAsync(Bitmap bitmap)
        {
            return await Task.Run(() => ExtractText(bitmap));
        }

        public string ExtractText(Bitmap bitmap)
        {
            try
            {
                if (_engine == null)
                {
                    throw new InvalidOperationException("OCR engine not initialized");
                }

                // Preprocess the image for better OCR results
                var processedBitmap = PreprocessImage(bitmap);

                // Convert bitmap to Tesseract format
                using (var pix = PixConverter.ToPix(processedBitmap))
                {
                    using (var page = _engine.Process(pix))
                    {
                        var text = page.GetText();
                        var confidence = page.GetMeanConfidence();

                        // Only return text if confidence is reasonable
                        if (confidence > 0.3f)
                        {
                            return text?.Trim() ?? string.Empty;
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"OCR processing failed: {ex.Message}", ex);
            }
        }

        private Bitmap PreprocessImage(Bitmap originalBitmap)
        {
            try
            {
                // Convert Bitmap to OpenCV Mat
                using (var mat = BitmapConverter.ToMat(originalBitmap))
                {
                    // Convert to grayscale
                    using (var gray = new Mat())
                    {
                        Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

                        // Apply Gaussian blur to reduce noise
                        using (var blurred = new Mat())
                        {
                            Cv2.GaussianBlur(gray, blurred, new OpenCvSharp.Size(5, 5), 0);

                            // Apply adaptive thresholding
                            using (var thresh = new Mat())
                            {
                                Cv2.AdaptiveThreshold(blurred, thresh, 255, AdaptiveThresholdTypes.GaussianC,
                                    ThresholdTypes.Binary, 11, 2);

                                // Apply morphological operations to clean up the image
                                using (var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OpenCvSharp.Size(2, 2)))
                                {
                                    using (var cleaned = new Mat())
                                    {
                                        Cv2.MorphologyEx(thresh, cleaned, MorphTypes.Close, kernel);

                                        // Convert back to Bitmap
                                        return BitmapConverter.ToBitmap(cleaned);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If preprocessing fails, return the original bitmap
                System.Diagnostics.Debug.WriteLine($"Image preprocessing failed: {ex.Message}");
                return originalBitmap;
            }
        }

        public async Task<string> ExtractTextFromRegionAsync(Bitmap bitmap, Rectangle region)
        {
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

                        return ExtractText(croppedBitmap);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to extract text from region: {ex.Message}", ex);
                }
            });
        }

        public async Task<string> ExtractTextFromFileAsync(string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (var bitmap = new Bitmap(imagePath))
                    {
                        return ExtractText(bitmap);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to extract text from file: {ex.Message}", ex);
                }
            });
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}