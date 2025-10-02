using System;
using System.Speech.Recognition;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AionDesktopAssistant.Services
{
    public class VoiceRecognitionService : IDisposable
    {
        private SpeechRecognitionEngine? _recognitionEngine;
        private bool _isListening = false;
        private bool _disposed = false;

        public event EventHandler<string>? CommandRecognized;

        public VoiceRecognitionService()
        {
            InitializeRecognitionEngine();
        }

        private void InitializeRecognitionEngine()
        {
            try
            {
                // Initialize the speech recognition engine
                _recognitionEngine = new SpeechRecognitionEngine(new CultureInfo("en-US"));

                // Configure recognition settings
                _recognitionEngine.SetInputToDefaultAudioDevice();

                // Create grammar for voice commands
                var commands = new Choices();

                // Basic navigation commands
                commands.Add("read screen");
                commands.Add("read this");
                commands.Add("what is on screen");
                commands.Add("describe screen");

                // Mouse commands
                commands.Add("click");
                commands.Add("click here");
                commands.Add("double click");
                commands.Add("right click");
                commands.Add("scroll up");
                commands.Add("scroll down");
                commands.Add("move mouse");

                // Keyboard commands
                commands.Add("type");
                commands.Add("press enter");
                commands.Add("press tab");
                commands.Add("press escape");
                commands.Add("press backspace");
                commands.Add("copy");
                commands.Add("paste");
                commands.Add("cut");
                commands.Add("select all");
                commands.Add("undo");
                commands.Add("save");

                // Window management
                commands.Add("switch window");
                commands.Add("close window");
                commands.Add("minimize window");
                commands.Add("maximize window");
                commands.Add("next window");
                commands.Add("previous window");
                commands.Add("show desktop");

                // Navigation
                commands.Add("go back");
                commands.Add("go forward");
                commands.Add("refresh");
                commands.Add("new tab");
                commands.Add("close tab");

                // System commands
                commands.Add("help");
                commands.Add("help me");
                commands.Add("stop listening");
                commands.Add("start listening");
                commands.Add("take screenshot");
                commands.Add("open calculator");
                commands.Add("open notepad");
                commands.Add("open file explorer");

                // Text input commands
                commands.Add("hello");
                commands.Add("thank you");
                commands.Add("yes");
                commands.Add("no");
                commands.Add("okay");
                commands.Add("cancel");
                commands.Add("delete");
                commands.Add("clear");

                // Dynamic text commands
                var textCommand = new GrammarBuilder("type");
                textCommand.Append(new DictationGrammar());

                var clickCommand = new GrammarBuilder("click at");
                clickCommand.Append(new Choices("one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "zero"));
                clickCommand.Append(new Choices("hundred", "thousand"));
                clickCommand.Append(new Choices("one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "zero"));
                clickCommand.Append(new Choices("hundred", "thousand"));

                var switchCommand = new GrammarBuilder("switch to");
                switchCommand.Append(new DictationGrammar());

                // Create grammars
                var basicGrammar = new Grammar(new GrammarBuilder(commands));
                var textGrammar = new Grammar(textCommand);
                var clickGrammar = new Grammar(clickCommand);
                var switchGrammar = new Grammar(switchCommand);
                var dictationGrammar = new DictationGrammar();

                // Load grammars
                _recognitionEngine.LoadGrammar(basicGrammar);
                _recognitionEngine.LoadGrammar(textGrammar);
                _recognitionEngine.LoadGrammar(clickGrammar);
                _recognitionEngine.LoadGrammar(switchGrammar);
                _recognitionEngine.LoadGrammar(dictationGrammar);

                // Configure recognition settings
                _recognitionEngine.InitialSilenceTimeout = TimeSpan.FromSeconds(3);
                _recognitionEngine.BabbleTimeout = TimeSpan.FromSeconds(2);
                _recognitionEngine.EndSilenceTimeout = TimeSpan.FromSeconds(1);

                // Attach event handlers
                _recognitionEngine.SpeechRecognized += OnSpeechRecognized;
                _recognitionEngine.SpeechDetected += OnSpeechDetected;
                _recognitionEngine.SpeechHypothesized += OnSpeechHypothesized;
                _recognitionEngine.RecognizeCompleted += OnRecognizeCompleted;

                System.Diagnostics.Debug.WriteLine("Voice recognition engine initialized successfully");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize voice recognition: {ex.Message}", ex);
            }
        }

        public void StartListening()
        {
            try
            {
                if (_recognitionEngine == null)
                    throw new InvalidOperationException("Recognition engine not initialized");

                if (_isListening)
                    return;

                _recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;

                System.Diagnostics.Debug.WriteLine("Voice recognition started");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start voice recognition: {ex.Message}", ex);
            }
        }

        public void StopListening()
        {
            try
            {
                if (_recognitionEngine == null || !_isListening)
                    return;

                _recognitionEngine.RecognizeAsyncStop();
                _isListening = false;

                System.Diagnostics.Debug.WriteLine("Voice recognition stopped");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to stop voice recognition: {ex.Message}", ex);
            }
        }

        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            try
            {
                if (e.Result.Confidence > 0.5) // Only process high-confidence results
                {
                    string recognizedText = e.Result.Text;
                    System.Diagnostics.Debug.WriteLine($"Voice recognized: {recognizedText} (Confidence: {e.Result.Confidence:F2})");

                    CommandRecognized?.Invoke(this, recognizedText);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Low confidence recognition ignored: {e.Result.Text} (Confidence: {e.Result.Confidence:F2})");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing speech recognition: {ex.Message}");
            }
        }

        private void OnSpeechDetected(object? sender, SpeechDetectedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Speech detected");
        }

        private void OnSpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Speech hypothesized: {e.Result.Text}");
        }

        private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                System.Diagnostics.Debug.WriteLine($"Recognition error: {e.Error.Message}");
            }

            if (e.Cancelled)
            {
                System.Diagnostics.Debug.WriteLine("Recognition cancelled");
            }

            if (e.InputStreamEnded)
            {
                System.Diagnostics.Debug.WriteLine("Recognition input stream ended");
            }
        }

        public bool IsListening => _isListening;

        public void SetConfidenceThreshold(float threshold)
        {
            // This would be used to adjust recognition sensitivity
            // Implementation depends on specific requirements
        }

        public async Task<string> RecognizeSingleCommandAsync(int timeoutSeconds = 10)
        {
            var tcs = new TaskCompletionSource<string>();

            EventHandler<string>? handler = null;
            handler = (sender, command) =>
            {
                CommandRecognized -= handler;
                tcs.SetResult(command);
            };

            CommandRecognized += handler;

            try
            {
                if (!_isListening)
                {
                    StartListening();
                }

                // Wait for recognition or timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    CommandRecognized -= handler;
                    throw new TimeoutException("Voice recognition timeout");
                }

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                CommandRecognized -= handler;
                throw new InvalidOperationException($"Single command recognition failed: {ex.Message}", ex);
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
                    if (_isListening)
                    {
                        StopListening();
                    }

                    _recognitionEngine?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing voice recognition: {ex.Message}");
                }

                _disposed = true;
            }
        }

        ~VoiceRecognitionService()
        {
            Dispose(false);
        }
    }
}