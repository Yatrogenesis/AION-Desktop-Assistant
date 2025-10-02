using System.Windows;
using System.Windows.Controls;
using AionDesktopAssistant.Services;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AionDesktopAssistant
{
    public partial class MainWindow : Window
    {
        private readonly ScreenCaptureService _screenCapture;
        private readonly OcrService _ocr;
        private readonly VoiceRecognitionService _voiceRecognition;
        private readonly VoiceSynthesisService _voiceSynthesis;
        private readonly MouseAutomationService _mouseAutomation;
        private readonly KeyboardAutomationService _keyboardAutomation;
        private readonly WindowManagementService _windowManagement;
        private readonly AccessibilityService _accessibility;

        private bool _isListening = false;

        public MainWindow(
            ScreenCaptureService screenCapture,
            OcrService ocr,
            VoiceRecognitionService voiceRecognition,
            VoiceSynthesisService voiceSynthesis,
            MouseAutomationService mouseAutomation,
            KeyboardAutomationService keyboardAutomation,
            WindowManagementService windowManagement,
            AccessibilityService accessibility)
        {
            InitializeComponent();

            _screenCapture = screenCapture;
            _ocr = ocr;
            _voiceRecognition = voiceRecognition;
            _voiceSynthesis = voiceSynthesis;
            _mouseAutomation = mouseAutomation;
            _keyboardAutomation = keyboardAutomation;
            _windowManagement = windowManagement;
            _accessibility = accessibility;

            InitializeServices();
        }

        private async void InitializeServices()
        {
            try
            {
                await _accessibility.InitializeAsync();
                _voiceRecognition.CommandRecognized += OnVoiceCommandRecognized;
                AddToOutput("✅ AION Desktop Assistant initialized successfully");
                StatusText.Text = "Ready";
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Initialization error: {ex.Message}");
                StatusText.Text = "Error";
            }
        }

        private async void OnVoiceCommandRecognized(object? sender, string command)
        {
            AddToOutput($"🎤 Voice command: {command}");
            VoiceInputText.Text = command;

            try
            {
                await ProcessVoiceCommand(command);
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Command processing error: {ex.Message}");
                await _voiceSynthesis.SpeakAsync($"Error processing command: {ex.Message}");
            }
        }

        private async Task ProcessVoiceCommand(string command)
        {
            command = command.ToLower().Trim();

            if (command.Contains("read screen"))
            {
                await ReadScreenContent();
            }
            else if (command.Contains("click"))
            {
                await ProcessClickCommand(command);
            }
            else if (command.Contains("type"))
            {
                await ProcessTypeCommand(command);
            }
            else if (command.Contains("switch to"))
            {
                await ProcessSwitchWindowCommand(command);
            }
            else if (command.Contains("help"))
            {
                await _voiceSynthesis.SpeakAsync("Available commands: read screen, click at position, type text, switch to window, help me");
            }
            else
            {
                AddToOutput($"⚠️ Unknown command: {command}");
                await _voiceSynthesis.SpeakAsync("Command not recognized. Say 'help' for available commands.");
            }
        }

        private async Task ProcessClickCommand(string command)
        {
            // Extract coordinates or use current mouse position
            if (command.Contains("at"))
            {
                // Try to extract coordinates
                var parts = command.Split(' ');
                if (parts.Length >= 3 && int.TryParse(parts[^2], out int x) && int.TryParse(parts[^1], out int y))
                {
                    _mouseAutomation.MoveMouse(x, y);
                    await Task.Delay(100);
                }
            }

            _mouseAutomation.Click();
            await _voiceSynthesis.SpeakAsync("Clicked");
        }

        private async Task ProcessTypeCommand(string command)
        {
            var textToType = command.Replace("type", "").Trim();
            if (!string.IsNullOrEmpty(textToType))
            {
                _keyboardAutomation.TypeText(textToType);
                await _voiceSynthesis.SpeakAsync($"Typed: {textToType}");
            }
        }

        private async Task ProcessSwitchWindowCommand(string command)
        {
            var windowName = command.Replace("switch to", "").Trim();
            if (!string.IsNullOrEmpty(windowName))
            {
                var switched = _windowManagement.SwitchToWindow(windowName);
                if (switched)
                {
                    await _voiceSynthesis.SpeakAsync($"Switched to {windowName}");
                }
                else
                {
                    await _voiceSynthesis.SpeakAsync($"Could not find window: {windowName}");
                }
            }
        }

        private async void StartListening_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _voiceRecognition.StartListening();
                _isListening = true;
                StartListeningBtn.IsEnabled = false;
                StopListeningBtn.IsEnabled = true;
                MicrophoneStatus.Text = "🎤 Microphone: Listening";
                StatusText.Text = "Listening";
                AddToOutput("🎤 Voice recognition started");
                await _voiceSynthesis.SpeakAsync("Voice control activated");
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Failed to start voice recognition: {ex.Message}");
            }
        }

        private async void StopListening_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _voiceRecognition.StopListening();
                _isListening = false;
                StartListeningBtn.IsEnabled = true;
                StopListeningBtn.IsEnabled = false;
                MicrophoneStatus.Text = "🎤 Microphone: Ready";
                StatusText.Text = "Ready";
                AddToOutput("🎤 Voice recognition stopped");
                await _voiceSynthesis.SpeakAsync("Voice control deactivated");
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Failed to stop voice recognition: {ex.Message}");
            }
        }

        private async void ReadScreen_Click(object sender, RoutedEventArgs e)
        {
            await ReadScreenContent();
        }

        private async Task ReadScreenContent()
        {
            try
            {
                AddToOutput("📖 Reading screen content...");
                StatusText.Text = "Reading Screen";

                var screenshot = _screenCapture.CaptureScreen();
                var text = await _ocr.ExtractTextAsync(screenshot);

                if (!string.IsNullOrEmpty(text))
                {
                    AddToOutput($"📄 Screen text: {text.Substring(0, Math.Min(text.Length, 200))}...");
                    await _voiceSynthesis.SpeakAsync($"Screen contains: {text.Substring(0, Math.Min(text.Length, 100))}");
                }
                else
                {
                    AddToOutput("📄 No text found on screen");
                    await _voiceSynthesis.SpeakAsync("No readable text found on screen");
                }

                StatusText.Text = "Ready";
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Screen reading error: {ex.Message}");
                StatusText.Text = "Error";
            }
        }

        private void CaptureScreen_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var screenshot = _screenCapture.CaptureScreen();
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var filename = $"screenshot_{timestamp}.png";

                _screenCapture.SaveScreenshot(screenshot, filename);
                AddToOutput($"📸 Screenshot saved: {filename}");
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Screenshot error: {ex.Message}");
            }
        }

        private void MoveMouse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (int.TryParse(MouseXText.Text, out int x) && int.TryParse(MouseYText.Text, out int y))
                {
                    _mouseAutomation.MoveMouse(x, y);
                    AddToOutput($"🖱️ Mouse moved to ({x}, {y})");
                }
                else
                {
                    AddToOutput("❌ Invalid coordinates");
                }
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Mouse movement error: {ex.Message}");
            }
        }

        private void ClickMouse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _mouseAutomation.Click();
                AddToOutput("🖱️ Mouse clicked");
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Mouse click error: {ex.Message}");
            }
        }

        private void ListWindows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var windows = _windowManagement.GetOpenWindows();
                WindowsComboBox.ItemsSource = windows;
                AddToOutput($"📝 Found {windows.Count} open windows");

                foreach (var window in windows.Take(10))
                {
                    AddToOutput($"  - {window}");
                }
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Window listing error: {ex.Message}");
            }
        }

        private async void SwitchWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WindowsComboBox.SelectedItem is string windowName)
                {
                    var switched = _windowManagement.SwitchToWindow(windowName);
                    if (switched)
                    {
                        AddToOutput($"🔄 Switched to: {windowName}");
                        await _voiceSynthesis.SpeakAsync($"Switched to {windowName}");
                    }
                    else
                    {
                        AddToOutput($"❌ Failed to switch to: {windowName}");
                    }
                }
            }
            catch (Exception ex)
            {
                AddToOutput($"❌ Window switch error: {ex.Message}");
            }
        }

        private void AddToOutput(string message)
        {
            Dispatcher.Invoke(() =>
            {
                OutputTextBox.Text += $"\n[{DateTime.Now:HH:mm:ss}] {message}";
                OutputTextBox.ScrollToEnd();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                if (_isListening)
                {
                    _voiceRecognition.StopListening();
                }
                _accessibility.Dispose();
            }
            catch (Exception ex)
            {
                // Log error but don't prevent closing
                System.Diagnostics.Debug.WriteLine($"Error during cleanup: {ex.Message}");
            }

            base.OnClosed(e);
        }
    }
}