using System;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using Serilog;
using System.Diagnostics;

namespace AionDesktopAssistant.Services
{
    public class VoiceSynthesisService : IDisposable
    {
        private static readonly ILogger _logger = Log.ForContext<VoiceSynthesisService>();
        private readonly SpeechSynthesizer _synthesizer;
        private bool _disposed = false;
        private int _speechCount = 0;
        private int _speechErrors = 0;

        public VoiceSynthesisService()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("🔊 Initializing VoiceSynthesisService...");

            try
            {
                _synthesizer = new SpeechSynthesizer();
                InitializeSynthesizer();

                stopwatch.Stop();
                _logger.Information("✅ VoiceSynthesisService initialized successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Fatal(ex, "❌ Failed to initialize VoiceSynthesisService after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private void InitializeSynthesizer()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔧 Initializing speech synthesizer...");

            try
            {
                // Configure the synthesizer
                _synthesizer.SetOutputToDefaultAudioDevice();
                _logger.Debug("🎧 Set output to default audio device");

                // Find the best available voice
                var voices = _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled && v.VoiceInfo.Culture.Name.StartsWith("en"))
                    .OrderByDescending(v => v.VoiceInfo.Gender == VoiceGender.Female)
                    .ThenBy(v => v.VoiceInfo.Age)
                    .ToList();

                _logger.Debug("🔍 Found {VoiceCount} enabled English voices", voices.Count);

                if (voices.Any())
                {
                    var selectedVoice = voices.First();
                    _synthesizer.SelectVoice(selectedVoice.VoiceInfo.Name);

                    _logger.Information("🎭 Selected voice: {VoiceName} ({Gender}, {Age}, {Culture})",
                        selectedVoice.VoiceInfo.Name,
                        selectedVoice.VoiceInfo.Gender,
                        selectedVoice.VoiceInfo.Age,
                        selectedVoice.VoiceInfo.Culture.Name);
                }
                else
                {
                    _logger.Warning("⚠️ No enabled English voices found, using default");
                }

                // Set default properties
                _synthesizer.Rate = 0; // Normal speed
                _synthesizer.Volume = 80; // 80% volume

                _logger.Debug("⚙️ Speech settings configured - Rate: {Rate}, Volume: {Volume}%",
                    _synthesizer.Rate, _synthesizer.Volume);

                // Attach event handlers
                _synthesizer.SpeakStarted += OnSpeakStarted;
                _synthesizer.SpeakCompleted += OnSpeakCompleted;
                _synthesizer.SpeakProgress += OnSpeakProgress;

                // Log all available voices
                var allVoices = _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => $"{v.VoiceInfo.Name} ({v.VoiceInfo.Culture.Name})")
                    .ToList();

                _logger.Debug("🎤 Available voices: {VoiceList}", string.Join(", ", allVoices));

                stopwatch.Stop();
                _logger.Information("✅ Speech synthesizer initialized successfully in {ElapsedMs}ms with {VoiceCount} voices",
                    stopwatch.ElapsedMilliseconds, allVoices.Count);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to initialize speech synthesizer after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to initialize voice synthesis: {ex.Message}", ex);
            }
        }

        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.Warning("⚠️ Attempted to speak null or empty text");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var textLength = text.Length;
            var textPreview = text.Length > 50 ? text[..50] + "..." : text;

            _logger.Debug("🗣️ Starting async speech - Length: {TextLength}, Preview: '{TextPreview}'",
                textLength, textPreview);

            try
            {
                var tcs = new TaskCompletionSource<bool>();

                EventHandler<SpeakCompletedEventArgs>? handler = null;
                handler = (sender, e) =>
                {
                    _synthesizer.SpeakCompleted -= handler;
                    if (e.Error != null)
                        tcs.SetException(e.Error);
                    else if (e.Cancelled)
                        tcs.SetCanceled();
                    else
                        tcs.SetResult(true);
                };

                _synthesizer.SpeakCompleted += handler;
                _synthesizer.SpeakAsync(text);

                await tcs.Task;

                _speechCount++;
                stopwatch.Stop();

                _logger.Information("✅ Async speech completed in {ElapsedMs}ms - Length: {TextLength}, Preview: '{TextPreview}'",
                    stopwatch.ElapsedMilliseconds, textLength, textPreview);
            }
            catch (Exception ex)
            {
                _speechErrors++;
                stopwatch.Stop();

                _logger.Error(ex, "❌ Async speech failed after {ElapsedMs}ms - Text: '{TextPreview}'",
                    stopwatch.ElapsedMilliseconds, textPreview);
                throw new InvalidOperationException($"Failed to speak text: {ex.Message}", ex);
            }
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.Warning("⚠️ Attempted to speak null or empty text");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var textLength = text.Length;
            var textPreview = text.Length > 50 ? text[..50] + "..." : text;

            _logger.Debug("🗣️ Starting synchronous speech - Length: {TextLength}, Preview: '{TextPreview}'",
                textLength, textPreview);

            try
            {
                _synthesizer.Speak(text);
                _speechCount++;
                stopwatch.Stop();

                _logger.Information("✅ Synchronous speech completed in {ElapsedMs}ms - Length: {TextLength}, Preview: '{TextPreview}'",
                    stopwatch.ElapsedMilliseconds, textLength, textPreview);
            }
            catch (Exception ex)
            {
                _speechErrors++;
                stopwatch.Stop();

                _logger.Error(ex, "❌ Synchronous speech failed after {ElapsedMs}ms - Text: '{TextPreview}'",
                    stopwatch.ElapsedMilliseconds, textPreview);
                throw new InvalidOperationException($"Failed to speak text: {ex.Message}", ex);
            }
        }

        public void SpeakAsync(string text, bool waitForCompletion = false)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.Warning("⚠️ Attempted to speak null or empty text");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var textLength = text.Length;
            var textPreview = text.Length > 50 ? text[..50] + "..." : text;

            _logger.Debug("🔄 Starting async speech (wait: {WaitForCompletion}) - Length: {TextLength}, Preview: '{TextPreview}'",
                waitForCompletion, textLength, textPreview);

            try
            {
                if (waitForCompletion)
                {
                    _synthesizer.Speak(text);
                    _speechCount++;
                    stopwatch.Stop();

                    _logger.Information("✅ Async speech with wait completed in {ElapsedMs}ms - Length: {TextLength}",
                        stopwatch.ElapsedMilliseconds, textLength);
                }
                else
                {
                    _synthesizer.SpeakAsync(text);
                    stopwatch.Stop();

                    _logger.Information("🚀 Async speech started in {ElapsedMs}ms - Length: {TextLength} (fire-and-forget)",
                        stopwatch.ElapsedMilliseconds, textLength);
                }
            }
            catch (Exception ex)
            {
                _speechErrors++;
                stopwatch.Stop();

                _logger.Error(ex, "❌ Async speech failed after {ElapsedMs}ms - Text: '{TextPreview}'",
                    stopwatch.ElapsedMilliseconds, textPreview);
                throw new InvalidOperationException($"Failed to speak text asynchronously: {ex.Message}", ex);
            }
        }

        public void StopSpeaking()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🛑 Stopping all speech...");

            try
            {
                _synthesizer.SpeakAsyncCancelAll();
                stopwatch.Stop();

                _logger.Information("✅ All speech stopped successfully in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Error stopping speech after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
        }

        public void SetRate(int rate)
        {
            var oldRate = _synthesizer.Rate;
            var clampedRate = Math.Max(-10, Math.Min(10, rate));

            _logger.Debug("🎚️ Setting speech rate from {OldRate} to {NewRate} (requested: {RequestedRate})",
                oldRate, clampedRate, rate);

            try
            {
                _synthesizer.Rate = clampedRate;

                _logger.Information("✅ Speech rate changed from {OldRate} to {NewRate}",
                    oldRate, clampedRate);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "❌ Failed to set speech rate to {Rate}", rate);
                throw new InvalidOperationException($"Failed to set speech rate: {ex.Message}", ex);
            }
        }

        public void SetVolume(int volume)
        {
            var oldVolume = _synthesizer.Volume;
            var clampedVolume = Math.Max(0, Math.Min(100, volume));

            _logger.Debug("🔊 Setting volume from {OldVolume}% to {NewVolume}% (requested: {RequestedVolume}%)",
                oldVolume, clampedVolume, volume);

            try
            {
                _synthesizer.Volume = clampedVolume;

                _logger.Information("✅ Volume changed from {OldVolume}% to {NewVolume}%",
                    oldVolume, clampedVolume);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "❌ Failed to set volume to {Volume}%", volume);
                throw new InvalidOperationException($"Failed to set speech volume: {ex.Message}", ex);
            }
        }

        public void SetVoice(string voiceName)
        {
            var currentVoice = _synthesizer.Voice.Name;
            _logger.Debug("🎭 Attempting to change voice from '{CurrentVoice}' to '{RequestedVoice}'",
                currentVoice, voiceName);

            try
            {
                var availableVoices = _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => v.VoiceInfo.Name)
                    .ToList();

                if (availableVoices.Contains(voiceName))
                {
                    _synthesizer.SelectVoice(voiceName);

                    _logger.Information("✅ Voice changed from '{OldVoice}' to '{NewVoice}'",
                        currentVoice, voiceName);
                }
                else
                {
                    _logger.Error("❌ Voice '{RequestedVoice}' not found. Available: {AvailableVoices}",
                        voiceName, string.Join(", ", availableVoices));
                    throw new ArgumentException($"Voice '{voiceName}' not found. Available voices: {string.Join(", ", availableVoices)}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "❌ Failed to set voice to '{VoiceName}'", voiceName);
                throw new InvalidOperationException($"Failed to set voice: {ex.Message}", ex);
            }
        }

        public string[] GetAvailableVoices()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("📋 Retrieving available voices...");

            try
            {
                var voices = _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => v.VoiceInfo.Name)
                    .ToArray();

                stopwatch.Stop();
                _logger.Information("✅ Retrieved {VoiceCount} available voices in {ElapsedMs}ms",
                    voices.Length, stopwatch.ElapsedMilliseconds);

                return voices;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to get available voices after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException($"Failed to get available voices: {ex.Message}", ex);
            }
        }

        public string GetCurrentVoice()
        {
            var voiceName = _synthesizer.Voice.Name;
            _logger.Debug("ℹ️ Current voice: {VoiceName}", voiceName);
            return voiceName;
        }

        public int GetCurrentRate()
        {
            var rate = _synthesizer.Rate;
            _logger.Debug("ℹ️ Current rate: {Rate}", rate);
            return rate;
        }

        public int GetCurrentVolume()
        {
            var volume = _synthesizer.Volume;
            _logger.Debug("ℹ️ Current volume: {Volume}%", volume);
            return volume;
        }

        public bool IsSpeaking()
        {
            var isSpeaking = _synthesizer.State == SynthesizerState.Speaking;
            _logger.Debug("ℹ️ Is speaking: {IsSpeaking}", isSpeaking);
            return isSpeaking;
        }

        // Predefined common responses
        public async Task SpeakWelcomeAsync()
        {
            var message = "AION Desktop Assistant is ready to help you. You can use voice commands to control your computer.";
            _logger.Information("👋 Speaking welcome message");
            await SpeakAsync(message);
        }

        public async Task SpeakErrorAsync(string error)
        {
            var message = $"Error: {error}";
            _logger.Information("⚠️ Speaking error message: {Error}", error);
            await SpeakAsync(message);
        }

        public async Task SpeakConfirmationAsync(string action)
        {
            var message = $"Action completed: {action}";
            _logger.Information("✅ Speaking confirmation for action: {Action}", action);
            await SpeakAsync(message);
        }

        public async Task SpeakHelpAsync()
        {
            var message = "Available commands include: read screen, click, type text, switch window, copy, paste, and many more. Say 'help me' for detailed assistance.";
            _logger.Information("❓ Speaking help message");
            await SpeakAsync(message);
        }

        public async Task SpeakScreenContentAsync(string content)
        {
            var originalLength = content.Length;
            if (content.Length > 200)
            {
                content = content.Substring(0, 200) + "... and more";
            }

            var message = $"Screen content: {content}";
            _logger.Information("📖 Speaking screen content - OriginalLength: {OriginalLength}, TruncatedLength: {TruncatedLength}",
                originalLength, content.Length);
            await SpeakAsync(message);
        }

        public async Task SpeakNavigationAsync(string location)
        {
            var message = $"Navigated to {location}";
            _logger.Information("🧭 Speaking navigation to: {Location}", location);
            await SpeakAsync(message);
        }

        public async Task SpeakTypingAsync(string text)
        {
            var originalLength = text.Length;
            string message;

            if (text.Length > 50)
            {
                message = $"Typed text starting with: {text.Substring(0, 50)}";
            }
            else
            {
                message = $"Typed: {text}";
            }

            _logger.Information("⌨️ Speaking typing confirmation - OriginalLength: {OriginalLength}",
                originalLength);
            await SpeakAsync(message);
        }

        // Event handlers
        private void OnSpeakStarted(object? sender, SpeakStartedEventArgs e)
        {
            _logger.Debug("🎤 Speech started");
        }

        private void OnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                _speechErrors++;
                _logger.Error(e.Error, "❌ Speech completed with error");
            }
            else if (e.Cancelled)
            {
                _logger.Information("🚫 Speech cancelled");
            }
            else
            {
                _logger.Debug("✅ Speech completed successfully");
            }
        }

        private void OnSpeakProgress(object? sender, SpeakProgressEventArgs e)
        {
            _logger.Debug("📢 Speaking progress: '{Text}' at position {AudioPosition}ms",
                e.Text, e.AudioPosition.TotalMilliseconds);
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
                _logger.Information("🗑️ Disposing VoiceSynthesisService...");

                try
                {
                    StopSpeaking();
                    _synthesizer?.Dispose();

                    _logger.Information("✅ VoiceSynthesisService disposed successfully - Final stats: {SpeechCount} speeches, {ErrorCount} errors",
                        _speechCount, _speechErrors);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "❌ Error disposing VoiceSynthesisService");
                }

                _disposed = true;
            }
        }

        ~VoiceSynthesisService()
        {
            Dispose(false);
        }
    }
}