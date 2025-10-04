using System;
using System.Speech.Recognition;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Serilog;
using System.Diagnostics;

namespace AionDesktopAssistant.Services
{
    public class VoiceRecognitionService : IDisposable
    {
        private static readonly ILogger _logger = Log.ForContext<VoiceRecognitionService>();
        private SpeechRecognitionEngine? _recognitionEngine;
        private bool _isListening = false;
        private bool _disposed = false;
        private float _confidenceThreshold = 0.5f;
        private int _commandsRecognized = 0;
        private int _commandsIgnored = 0;

        public event EventHandler<string>? CommandRecognized;

        public VoiceRecognitionService()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("🎤 Initializing VoiceRecognitionService...");

            try
            {
                InitializeRecognitionEngine();
                stopwatch.Stop();

                _logger.Information("✅ VoiceRecognitionService initialized successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Fatal(ex, "❌ Failed to initialize VoiceRecognitionService after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private void InitializeRecognitionEngine()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔧 Initializing speech recognition engine...");

            try
            {
                // Initialize the speech recognition engine
                _recognitionEngine = new SpeechRecognitionEngine(new CultureInfo("en-US"));
                _logger.Debug("🌐 Created SpeechRecognitionEngine with culture: en-US");

                // Configure recognition settings
                _recognitionEngine.SetInputToDefaultAudioDevice();
                _logger.Debug("🎧 Set input to default audio device");

                // Create grammar for voice commands
                var commands = new Choices();

                // Add all commands with logging
                var commandCategories = new Dictionary<string, string[]>
                {
                    ["Navigation"] = new[] { "read screen", "read this", "what is on screen", "describe screen" },
                    ["Mouse"] = new[] { "click", "click here", "double click", "right click", "scroll up", "scroll down", "move mouse" },
                    ["Keyboard"] = new[] { "type", "press enter", "press tab", "press escape", "press backspace", "copy", "paste", "cut", "select all", "undo", "save" },
                    ["Window"] = new[] { "switch window", "close window", "minimize window", "maximize window", "next window", "previous window", "show desktop" },
                    ["Browser"] = new[] { "go back", "go forward", "refresh", "new tab", "close tab" },
                    ["System"] = new[] { "help", "help me", "stop listening", "start listening", "take screenshot", "open calculator", "open notepad", "open file explorer" },
                    ["Text"] = new[] { "hello", "thank you", "yes", "no", "okay", "cancel", "delete", "clear" }
                };

                int totalCommands = 0;
                foreach (var category in commandCategories)
                {
                    foreach (var command in category.Value)
                    {
                        commands.Add(command);
                        totalCommands++;
                    }
                    _logger.Debug("📝 Added {Count} {Category} commands", category.Value.Length, category.Key);
                }

                _logger.Information("📚 Total voice commands loaded: {TotalCommands}", totalCommands);

                // Dynamic text commands
                var textCommand = new GrammarBuilder("type");
                textCommand.Append(new DictationGrammar());
                _logger.Debug("🗣️ Created dynamic text command grammar");

                var clickCommand = new GrammarBuilder("click at");
                clickCommand.Append(new Choices("one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "zero"));
                clickCommand.Append(new Choices("hundred", "thousand"));
                clickCommand.Append(new Choices("one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "zero"));
                clickCommand.Append(new Choices("hundred", "thousand"));
                _logger.Debug("🎯 Created coordinate click command grammar");

                var switchCommand = new GrammarBuilder("switch to");
                switchCommand.Append(new DictationGrammar());
                _logger.Debug("🔄 Created window switch command grammar");

                // Create grammars
                var grammars = new List<(Grammar grammar, string name)>
                {
                    (new Grammar(new GrammarBuilder(commands)), "Basic Commands"),
                    (new Grammar(textCommand), "Text Input"),
                    (new Grammar(clickCommand), "Click Coordinates"),
                    (new Grammar(switchCommand), "Window Switch"),
                    (new DictationGrammar(), "Free Dictation")
                };

                // Load grammars
                foreach (var (grammar, name) in grammars)
                {
                    _recognitionEngine.LoadGrammar(grammar);
                    _logger.Debug("📖 Loaded grammar: {GrammarName}", name);
                }

                // Configure recognition settings
                _recognitionEngine.InitialSilenceTimeout = TimeSpan.FromSeconds(3);
                _recognitionEngine.BabbleTimeout = TimeSpan.FromSeconds(2);
                _recognitionEngine.EndSilenceTimeout = TimeSpan.FromSeconds(1);

                _logger.Debug("⚙️ Recognition timeouts configured - Initial: 3s, Babble: 2s, EndSilence: 1s");

                // Attach event handlers
                _recognitionEngine.SpeechRecognized += OnSpeechRecognized;
                _recognitionEngine.SpeechDetected += OnSpeechDetected;
                _recognitionEngine.SpeechHypothesized += OnSpeechHypothesized;
                _recognitionEngine.RecognizeCompleted += OnRecognizeCompleted;

                stopwatch.Stop();
                _logger.Information("✅ Speech recognition engine initialized successfully in {ElapsedMs}ms with {GrammarCount} grammars",
                    stopwatch.ElapsedMilliseconds, grammars.Count);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to initialize voice recognition engine after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to initialize voice recognition: {ex.Message}", ex);
            }
        }

        public void StartListening()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🎧 Starting voice recognition...");

            try
            {
                if (_recognitionEngine == null)
                {
                    _logger.Error("❌ Recognition engine not initialized");
                    throw new InvalidOperationException("Recognition engine not initialized");
                }

                if (_isListening)
                {
                    _logger.Warning("⚠️ Voice recognition already listening");
                    return;
                }

                _recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
                _isListening = true;
                _commandsRecognized = 0;
                _commandsIgnored = 0;

                stopwatch.Stop();
                _logger.Information("✅ Voice recognition started successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to start voice recognition after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to start voice recognition: {ex.Message}", ex);
            }
        }

        public void StopListening()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🛑 Stopping voice recognition...");

            try
            {
                if (_recognitionEngine == null || !_isListening)
                {
                    _logger.Warning("⚠️ Voice recognition not currently listening");
                    return;
                }

                _recognitionEngine.RecognizeAsyncStop();
                _isListening = false;

                stopwatch.Stop();
                _logger.Information("✅ Voice recognition stopped in {ElapsedMs}ms - Session stats: {Recognized} recognized, {Ignored} ignored",
                    stopwatch.ElapsedMilliseconds, _commandsRecognized, _commandsIgnored);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to stop voice recognition after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to stop voice recognition: {ex.Message}", ex);
            }
        }

        private void OnSpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                string recognizedText = e.Result.Text;
                float confidence = e.Result.Confidence;

                _logger.Debug("🎯 Speech recognition result: '{Text}' (Confidence: {Confidence:P1})",
                    recognizedText, confidence);

                if (confidence > _confidenceThreshold)
                {
                    _commandsRecognized++;
                    stopwatch.Stop();

                    _logger.Information("✅ Voice command recognized in {ElapsedMs}ms: '{Command}' (Confidence: {Confidence:P1})",
                        stopwatch.ElapsedMilliseconds, recognizedText, confidence);

                    CommandRecognized?.Invoke(this, recognizedText);
                }
                else
                {
                    _commandsIgnored++;
                    stopwatch.Stop();

                    _logger.Warning("⚠️ Low confidence command ignored in {ElapsedMs}ms: '{Command}' (Confidence: {Confidence:P1}, Threshold: {Threshold:P1})",
                        stopwatch.ElapsedMilliseconds, recognizedText, confidence, _confidenceThreshold);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Error processing speech recognition after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        private void OnSpeechDetected(object? sender, SpeechDetectedEventArgs e)
        {
            _logger.Debug("👂 Speech detected - AudioPosition: {AudioPosition}ms", e.AudioPosition.TotalMilliseconds);
        }

        private void OnSpeechHypothesized(object? sender, SpeechHypothesizedEventArgs e)
        {
            _logger.Debug("💭 Speech hypothesis: '{Text}' (Confidence: {Confidence:P1})",
                e.Result.Text, e.Result.Confidence);
        }

        private void OnRecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _logger.Error(e.Error, "❌ Recognition completed with error");
            }

            if (e.Cancelled)
            {
                _logger.Information("🚫 Recognition cancelled");
            }

            if (e.InputStreamEnded)
            {
                _logger.Information("📡 Recognition input stream ended");
            }

            if (e.Error == null && !e.Cancelled && !e.InputStreamEnded)
            {
                _logger.Debug("✅ Recognition completed successfully");
            }
        }

        public bool IsListening => _isListening;

        public void SetConfidenceThreshold(float threshold)
        {
            var oldThreshold = _confidenceThreshold;
            _confidenceThreshold = threshold;

            _logger.Information("🎚️ Confidence threshold changed from {OldThreshold:P1} to {NewThreshold:P1}",
                oldThreshold, _confidenceThreshold);
        }

        public async Task<string> RecognizeSingleCommandAsync(int timeoutSeconds = 10)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🎯 Starting single command recognition with {Timeout}s timeout", timeoutSeconds);

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
                    _logger.Debug("🎧 Starting listening for single command");
                    StartListening();
                }

                // Wait for recognition or timeout
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));
                var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    CommandRecognized -= handler;
                    stopwatch.Stop();

                    _logger.Warning("⏰ Single command recognition timed out after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                    throw new TimeoutException("Voice recognition timeout");
                }

                var result = await tcs.Task;
                stopwatch.Stop();

                _logger.Information("✅ Single command recognition completed in {ElapsedMs}ms: '{Command}'",
                    stopwatch.ElapsedMilliseconds, result);

                return result;
            }
            catch (Exception ex)
            {
                CommandRecognized -= handler;
                stopwatch.Stop();

                _logger.Error(ex, "❌ Single command recognition failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
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
                _logger.Information("🗑️ Disposing VoiceRecognitionService...");

                try
                {
                    if (_isListening)
                    {
                        StopListening();
                    }

                    _recognitionEngine?.Dispose();

                    _logger.Information("✅ VoiceRecognitionService disposed successfully - Final stats: {Recognized} recognized, {Ignored} ignored",
                        _commandsRecognized, _commandsIgnored);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "❌ Error disposing VoiceRecognitionService");
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