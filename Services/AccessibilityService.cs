using System;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AionDesktopAssistant.Services
{
    public class AccessibilityService : IDisposable
    {
        private readonly ScreenCaptureService _screenCapture;
        private readonly OcrService _ocr;
        private readonly VoiceRecognitionService _voiceRecognition;
        private readonly VoiceSynthesisService _voiceSynthesis;
        private readonly MouseAutomationService _mouseAutomation;
        private readonly KeyboardAutomationService _keyboardAutomation;
        private readonly WindowManagementService _windowManagement;

        private bool _isInitialized = false;
        private bool _disposed = false;
        private CancellationTokenSource? _cancellationTokenSource;

        public AccessibilityService(
            ScreenCaptureService screenCapture,
            OcrService ocr,
            VoiceRecognitionService voiceRecognition,
            VoiceSynthesisService voiceSynthesis,
            MouseAutomationService mouseAutomation,
            KeyboardAutomationService keyboardAutomation,
            WindowManagementService windowManagement)
        {
            _screenCapture = screenCapture;
            _ocr = ocr;
            _voiceRecognition = voiceRecognition;
            _voiceSynthesis = voiceSynthesis;
            _mouseAutomation = mouseAutomation;
            _keyboardAutomation = keyboardAutomation;
            _windowManagement = windowManagement;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();

                // Initialize UI Automation
                AutomationElement.RootElement.SetFocus();

                _isInitialized = true;

                await _voiceSynthesis.SpeakWelcomeAsync();
                System.Diagnostics.Debug.WriteLine("Accessibility service initialized successfully");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize accessibility service: {ex.Message}", ex);
            }
        }

        public async Task<string> AnalyzeScreenAsync()
        {
            try
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Service not initialized");

                // Capture screen
                var screenshot = _screenCapture.CaptureScreen();

                // Extract text using OCR
                var ocrText = await _ocr.ExtractTextAsync(screenshot);

                // Get UI automation elements
                var uiElements = GetUIElements();

                // Combine OCR and UI automation results
                var analysis = $"Screen analysis:\n";

                if (!string.IsNullOrEmpty(ocrText))
                {
                    analysis += $"Visible text: {ocrText.Substring(0, Math.Min(ocrText.Length, 300))}\n";
                }

                if (uiElements.Any())
                {
                    analysis += $"Interactive elements found: {uiElements.Count}\n";
                    foreach (var element in uiElements.Take(5))
                    {
                        analysis += $"- {element}\n";
                    }
                }

                // Get current window information
                var currentWindow = _windowManagement.GetCurrentWindowTitle();
                if (!string.IsNullOrEmpty(currentWindow))
                {
                    analysis += $"Current window: {currentWindow}\n";
                }

                return analysis;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to analyze screen: {ex.Message}", ex);
            }
        }

        public async Task ReadScreenAloudAsync()
        {
            try
            {
                var analysis = await AnalyzeScreenAsync();
                await _voiceSynthesis.SpeakScreenContentAsync(analysis);
            }
            catch (Exception ex)
            {
                await _voiceSynthesis.SpeakErrorAsync($"Could not read screen: {ex.Message}");
            }
        }

        public async Task PerformAccessibilityActionAsync(string action, string? parameter = null)
        {
            try
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Service not initialized");

                switch (action.ToLower())
                {
                    case "read_screen":
                        await ReadScreenAloudAsync();
                        break;

                    case "click_element":
                        await ClickAccessibleElementAsync(parameter);
                        break;

                    case "type_text":
                        if (!string.IsNullOrEmpty(parameter))
                        {
                            _keyboardAutomation.TypeText(parameter);
                            await _voiceSynthesis.SpeakTypingAsync(parameter);
                        }
                        break;

                    case "navigate_window":
                        if (!string.IsNullOrEmpty(parameter))
                        {
                            var switched = _windowManagement.SwitchToWindow(parameter);
                            if (switched)
                            {
                                await _voiceSynthesis.SpeakNavigationAsync(parameter);
                            }
                            else
                            {
                                await _voiceSynthesis.SpeakErrorAsync($"Could not find window: {parameter}");
                            }
                        }
                        break;

                    case "take_screenshot":
                        await TakeAnnotatedScreenshotAsync();
                        break;

                    case "help":
                        await _voiceSynthesis.SpeakHelpAsync();
                        break;

                    default:
                        await _voiceSynthesis.SpeakErrorAsync($"Unknown action: {action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                await _voiceSynthesis.SpeakErrorAsync($"Action failed: {ex.Message}");
            }
        }

        public async Task<List<AccessibleElement>> FindAccessibleElementsAsync(string searchTerm)
        {
            try
            {
                var elements = new List<AccessibleElement>();

                // Search through UI Automation tree
                var rootElement = AutomationElement.RootElement;
                var condition = new PropertyCondition(AutomationElement.NameProperty, searchTerm);
                var foundElements = rootElement.FindAll(TreeScope.Descendants, condition);

                foreach (AutomationElement element in foundElements)
                {
                    try
                    {
                        var accessibleElement = new AccessibleElement
                        {
                            Name = element.Current.Name,
                            Type = element.Current.ControlType.ProgrammaticName,
                            BoundingRectangle = element.Current.BoundingRectangle,
                            IsEnabled = element.Current.IsEnabled,
                            HasKeyboardFocus = element.Current.HasKeyboardFocus,
                            AutomationId = element.Current.AutomationId
                        };

                        elements.Add(accessibleElement);
                    }
                    catch (Exception)
                    {
                        // Element might be stale, skip it
                        continue;
                    }
                }

                return elements;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to find accessible elements: {ex.Message}", ex);
            }
        }

        public async Task ClickAccessibleElementAsync(string? elementName)
        {
            try
            {
                if (string.IsNullOrEmpty(elementName))
                    return;

                var elements = await FindAccessibleElementsAsync(elementName);
                var clickableElement = elements.FirstOrDefault(e =>
                    e.IsEnabled &&
                    (e.Type.Contains("Button") || e.Type.Contains("Link") || e.Type.Contains("MenuItem")));

                if (clickableElement != null)
                {
                    var centerX = (int)(clickableElement.BoundingRectangle.Left + clickableElement.BoundingRectangle.Width / 2);
                    var centerY = (int)(clickableElement.BoundingRectangle.Top + clickableElement.BoundingRectangle.Height / 2);

                    _mouseAutomation.Click(centerX, centerY);
                    await _voiceSynthesis.SpeakConfirmationAsync($"Clicked {elementName}");
                }
                else
                {
                    await _voiceSynthesis.SpeakErrorAsync($"Could not find clickable element: {elementName}");
                }
            }
            catch (Exception ex)
            {
                await _voiceSynthesis.SpeakErrorAsync($"Failed to click element: {ex.Message}");
            }
        }

        public async Task TakeAnnotatedScreenshotAsync()
        {
            try
            {
                var screenshot = _screenCapture.CaptureScreen();
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"accessibility_screenshot_{timestamp}.png";

                // Add annotations for accessible elements
                var annotatedScreenshot = await AnnotateScreenshotAsync(screenshot);

                _screenCapture.SaveScreenshot(annotatedScreenshot, filename);
                await _voiceSynthesis.SpeakConfirmationAsync($"Annotated screenshot saved: {filename}");
            }
            catch (Exception ex)
            {
                await _voiceSynthesis.SpeakErrorAsync($"Failed to take annotated screenshot: {ex.Message}");
            }
        }

        private async Task<Bitmap> AnnotateScreenshotAsync(Bitmap originalScreenshot)
        {
            try
            {
                var annotated = new Bitmap(originalScreenshot);
                using (var graphics = Graphics.FromImage(annotated))
                {
                    var pen = new Pen(Color.Red, 2);
                    var font = new Font("Arial", 10);
                    var brush = new SolidBrush(Color.Red);

                    // Find and annotate UI elements
                    var rootElement = AutomationElement.RootElement;
                    var condition = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
                    var elements = rootElement.FindAll(TreeScope.Descendants, condition);

                    int elementCount = 0;
                    foreach (AutomationElement element in elements)
                    {
                        try
                        {
                            var rect = element.Current.BoundingRectangle;
                            if (rect.Width > 10 && rect.Height > 10 && elementCount < 20) // Limit annotations
                            {
                                var drawRect = new Rectangle((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height);
                                graphics.DrawRectangle(pen, drawRect);

                                var name = element.Current.Name;
                                if (!string.IsNullOrEmpty(name) && name.Length < 20)
                                {
                                    graphics.DrawString(name, font, brush, (float)rect.Left, (float)rect.Top - 15);
                                }

                                elementCount++;
                            }
                        }
                        catch (Exception)
                        {
                            // Element might be stale, skip it
                            continue;
                        }
                    }

                    pen.Dispose();
                    font.Dispose();
                    brush.Dispose();
                }

                return annotated;
            }
            catch (Exception)
            {
                // If annotation fails, return original screenshot
                return originalScreenshot;
            }
        }

        private List<string> GetUIElements()
        {
            try
            {
                var elements = new List<string>();
                var rootElement = AutomationElement.RootElement;
                var condition = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
                var foundElements = rootElement.FindAll(TreeScope.Descendants, condition);

                foreach (AutomationElement element in foundElements)
                {
                    try
                    {
                        var name = element.Current.Name;
                        var type = element.Current.ControlType.ProgrammaticName;

                        if (!string.IsNullOrEmpty(name) && name.Length < 50)
                        {
                            elements.Add($"{type}: {name}");
                        }

                        if (elements.Count >= 10) break; // Limit results
                    }
                    catch (Exception)
                    {
                        // Element might be stale, skip it
                        continue;
                    }
                }

                return elements;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _cancellationTokenSource?.Cancel();
                    _cancellationTokenSource?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing accessibility service: {ex.Message}");
                }

                _disposed = true;
            }
        }

        ~AccessibilityService()
        {
            Dispose(false);
        }
    }

    public class AccessibleElement
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public System.Windows.Rect BoundingRectangle { get; set; }
        public bool IsEnabled { get; set; }
        public bool HasKeyboardFocus { get; set; }
        public string AutomationId { get; set; } = string.Empty;
    }
}