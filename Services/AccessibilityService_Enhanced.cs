using System;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Automation;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Serilog;
using System.Diagnostics;

namespace AionDesktopAssistant.Services
{
    public class AccessibilityService : IDisposable
    {
        private static readonly ILogger _logger = Log.ForContext<AccessibilityService>();

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

        private int _actionCount = 0;
        private int _errorCount = 0;
        private int _screenAnalysisCount = 0;
        private int _elementInteractionCount = 0;

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

            _logger.Information("♿ AccessibilityService constructed with all dependencies injected");
            _logger.Debug("🔧 Service dependencies - ScreenCapture: {HasScreenCapture}, OCR: {HasOcr}, Voice: {HasVoice}, Mouse: {HasMouse}, Keyboard: {HasKeyboard}, Windows: {HasWindows}",
                _screenCapture != null, _ocr != null, _voiceRecognition != null && _voiceSynthesis != null,
                _mouseAutomation != null, _keyboardAutomation != null, _windowManagement != null);
        }

        public async Task InitializeAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("🚀 Initializing AccessibilityService...");

            try
            {
                if (_isInitialized)
                {
                    _logger.Warning("⚠️ AccessibilityService already initialized, skipping");
                    return;
                }

                _logger.Debug("🔧 Creating cancellation token source");
                _cancellationTokenSource = new CancellationTokenSource();

                _logger.Debug("🖥️ Initializing UI Automation root element");
                var rootElement = AutomationElement.RootElement;
                rootElement.SetFocus();
                _logger.Debug("✅ UI Automation root element focused");

                _isInitialized = true;
                stopwatch.Stop();

                _logger.Information("✅ AccessibilityService initialized successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                _logger.Debug("🎤 Speaking welcome message");
                await _voiceSynthesis.SpeakWelcomeAsync();

                _logger.Information("🎉 AccessibilityService fully operational and ready to assist users");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to initialize AccessibilityService after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to initialize accessibility service: {ex.Message}", ex);
            }
        }

        public async Task<string> AnalyzeScreenAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Starting comprehensive screen analysis...");

            try
            {
                if (!_isInitialized)
                {
                    _logger.Error("❌ AccessibilityService not initialized for screen analysis");
                    throw new InvalidOperationException("Service not initialized");
                }

                _logger.Debug("📸 Capturing current screen state");
                var screenshot = _screenCapture.CaptureScreen();
                _logger.Debug("✅ Screen captured for analysis");

                _logger.Debug("📝 Extracting text using OCR");
                var ocrText = await _ocr.ExtractTextAsync(screenshot);
                var ocrLength = ocrText?.Length ?? 0;
                _logger.Debug("✅ OCR completed - Extracted {TextLength} characters", ocrLength);

                _logger.Debug("🔍 Analyzing UI automation elements");
                var uiElements = GetUIElements();
                _logger.Debug("✅ UI analysis completed - Found {ElementCount} interactive elements", uiElements.Count);

                _logger.Debug("🪟 Getting current window information");
                var currentWindow = _windowManagement.GetCurrentWindowTitle();
                _logger.Debug("✅ Current window identified: '{WindowTitle}'", currentWindow);

                // Build comprehensive analysis
                var analysis = "Screen analysis:\n";

                if (!string.IsNullOrEmpty(ocrText))
                {
                    var textPreview = ocrText.Length > 300 ? ocrText.Substring(0, 300) + "..." : ocrText;
                    analysis += $"Visible text: {textPreview}\n";
                    _logger.Debug("📄 Text content preview: '{TextPreview}'", textPreview.Substring(0, Math.Min(100, textPreview.Length)));
                }

                if (uiElements.Any())
                {
                    analysis += $"Interactive elements found: {uiElements.Count}\n";
                    var elementsList = uiElements.Take(5).ToList();
                    foreach (var element in elementsList)
                    {
                        analysis += $"- {element}\n";
                    }
                    _logger.Debug("🎯 Top interactive elements: {ElementsList}", string.Join(", ", elementsList));
                }

                if (!string.IsNullOrEmpty(currentWindow))
                {
                    analysis += $"Current window: {currentWindow}\n";
                }

                _screenAnalysisCount++;
                stopwatch.Stop();

                _logger.Information("✅ Screen analysis completed in {ElapsedMs}ms - OCR: {OcrChars} chars, UI: {UiElements} elements, Window: '{Window}', Total analyses: {TotalCount}",
                    stopwatch.ElapsedMilliseconds, ocrLength, uiElements.Count, currentWindow, _screenAnalysisCount);

                return analysis;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Screen analysis failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to analyze screen: {ex.Message}", ex);
            }
        }

        public async Task ReadScreenAloudAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🎤 Starting screen reading process...");

            try
            {
                _logger.Debug("🔍 Performing screen analysis for voice output");
                var analysis = await AnalyzeScreenAsync();

                _logger.Debug("🎵 Converting screen analysis to speech");
                await _voiceSynthesis.SpeakScreenContentAsync(analysis);

                stopwatch.Stop();
                _logger.Information("✅ Screen reading completed successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Screen reading failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

                _logger.Debug("🎤 Speaking error message to user");
                await _voiceSynthesis.SpeakErrorAsync($"Could not read screen: {ex.Message}");
            }
        }

        public async Task PerformAccessibilityActionAsync(string action, string? parameter = null)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("🎯 Performing accessibility action: '{Action}' with parameter: '{Parameter}'",
                action, parameter ?? "none");

            try
            {
                if (!_isInitialized)
                {
                    _logger.Error("❌ AccessibilityService not initialized for action execution");
                    throw new InvalidOperationException("Service not initialized");
                }

                var actionLower = action.ToLower();
                _logger.Debug("🔄 Processing action: {Action}", actionLower);

                switch (actionLower)
                {
                    case "read_screen":
                        _logger.Debug("📖 Executing read_screen action");
                        await ReadScreenAloudAsync();
                        break;

                    case "click_element":
                        _logger.Debug("🖱️ Executing click_element action with target: '{Target}'", parameter);
                        await ClickAccessibleElementAsync(parameter);
                        break;

                    case "type_text":
                        if (!string.IsNullOrEmpty(parameter))
                        {
                            _logger.Debug("⌨️ Executing type_text action - Length: {Length} characters", parameter.Length);
                            _keyboardAutomation.TypeText(parameter);
                            await _voiceSynthesis.SpeakTypingAsync(parameter);
                            _logger.Information("✅ Text typed successfully: '{Text}'", parameter);
                        }
                        else
                        {
                            _logger.Warning("⚠️ type_text action called without text parameter");
                        }
                        break;

                    case "navigate_window":
                        if (!string.IsNullOrEmpty(parameter))
                        {
                            _logger.Debug("🪟 Executing navigate_window action to: '{WindowTitle}'", parameter);
                            var switched = _windowManagement.SwitchToWindow(parameter);
                            if (switched)
                            {
                                await _voiceSynthesis.SpeakNavigationAsync(parameter);
                                _logger.Information("✅ Successfully navigated to window: '{WindowTitle}'", parameter);
                            }
                            else
                            {
                                await _voiceSynthesis.SpeakErrorAsync($"Could not find window: {parameter}");
                                _logger.Warning("⚠️ Failed to find window: '{WindowTitle}'", parameter);
                            }
                        }
                        else
                        {
                            _logger.Warning("⚠️ navigate_window action called without window parameter");
                        }
                        break;

                    case "take_screenshot":
                        _logger.Debug("📷 Executing take_screenshot action");
                        await TakeAnnotatedScreenshotAsync();
                        break;

                    case "help":
                        _logger.Debug("❓ Executing help action");
                        await _voiceSynthesis.SpeakHelpAsync();
                        break;

                    default:
                        _logger.Warning("⚠️ Unknown action requested: '{Action}'", action);
                        await _voiceSynthesis.SpeakErrorAsync($"Unknown action: {action}");
                        break;
                }

                _actionCount++;
                stopwatch.Stop();

                _logger.Information("✅ Accessibility action '{Action}' completed in {ElapsedMs}ms - Total actions: {TotalActions}",
                    action, stopwatch.ElapsedMilliseconds, _actionCount);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Accessibility action '{Action}' failed after {ElapsedMs}ms",
                    action, stopwatch.ElapsedMilliseconds);
                await _voiceSynthesis.SpeakErrorAsync($"Action failed: {ex.Message}");
            }
        }

        public async Task<List<AccessibleElement>> FindAccessibleElementsAsync(string searchTerm)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Searching for accessible elements matching: '{SearchTerm}'", searchTerm);

            try
            {
                var elements = new List<AccessibleElement>();

                _logger.Debug("🌳 Traversing UI Automation tree");
                var rootElement = AutomationElement.RootElement;
                var condition = new PropertyCondition(AutomationElement.NameProperty, searchTerm);
                var foundElements = rootElement.FindAll(TreeScope.Descendants, condition);

                _logger.Debug("📋 Processing {ElementCount} potential matches", foundElements.Count);

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
                        _logger.Debug("✅ Element found: '{Name}' ({Type}) - Enabled: {IsEnabled}, Bounds: {Bounds}",
                            accessibleElement.Name, accessibleElement.Type, accessibleElement.IsEnabled,
                            $"{accessibleElement.BoundingRectangle.Width}x{accessibleElement.BoundingRectangle.Height}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug("⚠️ Skipping stale UI element: {Error}", ex.Message);
                        continue;
                    }
                }

                stopwatch.Stop();

                _logger.Information("✅ Element search completed in {ElapsedMs}ms - Found {ElementCount} accessible elements for '{SearchTerm}'",
                    stopwatch.ElapsedMilliseconds, elements.Count, searchTerm);

                return elements;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Element search failed after {ElapsedMs}ms for term: '{SearchTerm}'",
                    stopwatch.ElapsedMilliseconds, searchTerm);
                throw new InvalidOperationException($"Failed to find accessible elements: {ex.Message}", ex);
            }
        }

        public async Task ClickAccessibleElementAsync(string? elementName)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🖱️ Attempting to click accessible element: '{ElementName}'", elementName);

            try
            {
                if (string.IsNullOrEmpty(elementName))
                {
                    _logger.Warning("⚠️ Click attempt with null or empty element name");
                    return;
                }

                _logger.Debug("🔍 Searching for clickable elements matching: '{ElementName}'", elementName);
                var elements = await FindAccessibleElementsAsync(elementName);

                var clickableElement = elements.FirstOrDefault(e =>
                    e.IsEnabled &&
                    (e.Type.Contains("Button") || e.Type.Contains("Link") || e.Type.Contains("MenuItem")));

                if (clickableElement != null)
                {
                    var centerX = (int)(clickableElement.BoundingRectangle.Left + clickableElement.BoundingRectangle.Width / 2);
                    var centerY = (int)(clickableElement.BoundingRectangle.Top + clickableElement.BoundingRectangle.Height / 2);

                    _logger.Debug("🎯 Clicking element at coordinates: ({X}, {Y}) - Element: '{Name}' ({Type})",
                        centerX, centerY, clickableElement.Name, clickableElement.Type);

                    _mouseAutomation.Click(centerX, centerY);
                    await _voiceSynthesis.SpeakConfirmationAsync($"Clicked {elementName}");

                    _elementInteractionCount++;
                    stopwatch.Stop();

                    _logger.Information("✅ Successfully clicked element '{ElementName}' in {ElapsedMs}ms - Total interactions: {TotalInteractions}",
                        elementName, stopwatch.ElapsedMilliseconds, _elementInteractionCount);
                }
                else
                {
                    stopwatch.Stop();
                    _logger.Warning("⚠️ No clickable element found for '{ElementName}' (searched in {ElapsedMs}ms)",
                        elementName, stopwatch.ElapsedMilliseconds);
                    await _voiceSynthesis.SpeakErrorAsync($"Could not find clickable element: {elementName}");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to click element '{ElementName}' after {ElapsedMs}ms",
                    elementName, stopwatch.ElapsedMilliseconds);
                await _voiceSynthesis.SpeakErrorAsync($"Failed to click element: {ex.Message}");
            }
        }

        public async Task TakeAnnotatedScreenshotAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("📷 Taking annotated screenshot for accessibility analysis...");

            try
            {
                _logger.Debug("📸 Capturing base screenshot");
                var screenshot = _screenCapture.CaptureScreen();

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"accessibility_screenshot_{timestamp}.png";
                _logger.Debug("🏷️ Generated filename: {Filename}", filename);

                _logger.Debug("🖊️ Adding accessibility annotations to screenshot");
                var annotatedScreenshot = await AnnotateScreenshotAsync(screenshot);

                _logger.Debug("💾 Saving annotated screenshot");
                _screenCapture.SaveScreenshot(annotatedScreenshot, filename);
                await _voiceSynthesis.SpeakConfirmationAsync($"Annotated screenshot saved: {filename}");

                stopwatch.Stop();

                _logger.Information("✅ Annotated screenshot saved successfully in {ElapsedMs}ms - Filename: {Filename}",
                    stopwatch.ElapsedMilliseconds, filename);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _errorCount++;
                _logger.Error(ex, "❌ Failed to take annotated screenshot after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                await _voiceSynthesis.SpeakErrorAsync($"Failed to take annotated screenshot: {ex.Message}");
            }
        }

        private async Task<Bitmap> AnnotateScreenshotAsync(Bitmap originalScreenshot)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🖊️ Starting screenshot annotation process...");

            try
            {
                var annotated = new Bitmap(originalScreenshot);
                using (var graphics = Graphics.FromImage(annotated))
                {
                    var pen = new Pen(Color.Red, 2);
                    var font = new Font("Arial", 10);
                    var brush = new SolidBrush(Color.Red);

                    _logger.Debug("🔍 Finding UI elements for annotation");
                    var rootElement = AutomationElement.RootElement;
                    var condition = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
                    var elements = rootElement.FindAll(TreeScope.Descendants, condition);

                    int elementCount = 0;
                    int annotatedCount = 0;
                    const int maxAnnotations = 20;

                    _logger.Debug("📋 Processing {ElementCount} elements for annotation (max: {MaxAnnotations})",
                        elements.Count, maxAnnotations);

                    foreach (AutomationElement element in elements)
                    {
                        elementCount++;
                        try
                        {
                            var rect = element.Current.BoundingRectangle;
                            if (rect.Width > 10 && rect.Height > 10 && annotatedCount < maxAnnotations)
                            {
                                var drawRect = new Rectangle((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height);
                                graphics.DrawRectangle(pen, drawRect);

                                var name = element.Current.Name;
                                if (!string.IsNullOrEmpty(name) && name.Length < 20)
                                {
                                    graphics.DrawString(name, font, brush, (float)rect.Left, (float)rect.Top - 15);
                                }

                                annotatedCount++;
                                _logger.Debug("🏷️ Annotated element {Count}: '{Name}' at ({X}, {Y})",
                                    annotatedCount, name, (int)rect.Left, (int)rect.Top);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Debug("⚠️ Skipping stale element during annotation: {Error}", ex.Message);
                            continue;
                        }
                    }

                    pen.Dispose();
                    font.Dispose();
                    brush.Dispose();

                    stopwatch.Stop();

                    _logger.Information("✅ Screenshot annotation completed in {ElapsedMs}ms - Processed: {ProcessedCount}, Annotated: {AnnotatedCount}",
                        stopwatch.ElapsedMilliseconds, elementCount, annotatedCount);
                }

                return annotated;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Warning(ex, "⚠️ Screenshot annotation failed after {ElapsedMs}ms, returning original screenshot",
                    stopwatch.ElapsedMilliseconds);
                return originalScreenshot;
            }
        }

        private List<string> GetUIElements()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔍 Collecting UI elements for analysis...");

            try
            {
                var elements = new List<string>();
                var rootElement = AutomationElement.RootElement;
                var condition = new PropertyCondition(AutomationElement.IsEnabledProperty, true);
                var foundElements = rootElement.FindAll(TreeScope.Descendants, condition);

                int processedCount = 0;
                const int maxElements = 10;

                _logger.Debug("📋 Processing {ElementCount} elements (max: {MaxElements})",
                    foundElements.Count, maxElements);

                foreach (AutomationElement element in foundElements)
                {
                    processedCount++;
                    try
                    {
                        var name = element.Current.Name;
                        var type = element.Current.ControlType.ProgrammaticName;

                        if (!string.IsNullOrEmpty(name) && name.Length < 50)
                        {
                            var elementInfo = $"{type}: {name}";
                            elements.Add(elementInfo);
                            _logger.Debug("📝 UI element {Count}: {ElementInfo}", elements.Count, elementInfo);
                        }

                        if (elements.Count >= maxElements) break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Debug("⚠️ Skipping stale UI element: {Error}", ex.Message);
                        continue;
                    }
                }

                stopwatch.Stop();

                _logger.Debug("✅ UI element collection completed in {ElapsedMs}ms - Processed: {ProcessedCount}, Collected: {CollectedCount}",
                    stopwatch.ElapsedMilliseconds, processedCount, elements.Count);

                return elements;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Warning(ex, "⚠️ UI element collection failed after {ElapsedMs}ms, returning empty list",
                    stopwatch.ElapsedMilliseconds);
                return new List<string>();
            }
        }

        public void LogStatistics()
        {
            _logger.Information("📊 AccessibilityService statistics - Actions: {ActionCount}, Screen analyses: {ScreenCount}, Element interactions: {ElementCount}, Errors: {ErrorCount}",
                _actionCount, _screenAnalysisCount, _elementInteractionCount, _errorCount);
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
                var stopwatch = Stopwatch.StartNew();
                _logger.Information("🧹 Disposing AccessibilityService...");

                try
                {
                    if (_cancellationTokenSource != null)
                    {
                        _logger.Debug("🛑 Cancelling ongoing operations");
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();
                    }

                    LogStatistics();
                    stopwatch.Stop();

                    _logger.Information("✅ AccessibilityService disposed successfully in {ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.Error(ex, "❌ Error disposing AccessibilityService after {ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                }

                _disposed = true;
            }
        }

        ~AccessibilityService()
        {
            _logger.Debug("🗑️ AccessibilityService finalizer called");
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