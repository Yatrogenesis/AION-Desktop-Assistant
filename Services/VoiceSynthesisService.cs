using System;
using System.Speech.Synthesis;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;

namespace AionDesktopAssistant.Services
{
    public class VoiceSynthesisService : IDisposable
    {
        private readonly SpeechSynthesizer _synthesizer;
        private bool _disposed = false;

        public VoiceSynthesisService()
        {
            _synthesizer = new SpeechSynthesizer();
            InitializeSynthesizer();
        }

        private void InitializeSynthesizer()
        {
            try
            {
                // Configure the synthesizer
                _synthesizer.SetOutputToDefaultAudioDevice();

                // Find the best available voice
                var voices = _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled && v.VoiceInfo.Culture.Name.StartsWith("en"))
                    .OrderByDescending(v => v.VoiceInfo.Gender == VoiceGender.Female)
                    .ThenBy(v => v.VoiceInfo.Age)
                    .ToList();

                if (voices.Any())
                {
                    _synthesizer.SelectVoice(voices.First().VoiceInfo.Name);
                    System.Diagnostics.Debug.WriteLine($"Selected voice: {voices.First().VoiceInfo.Name}");
                }

                // Set default properties
                _synthesizer.Rate = 0; // Normal speed
                _synthesizer.Volume = 80; // 80% volume

                // Attach event handlers
                _synthesizer.SpeakStarted += OnSpeakStarted;
                _synthesizer.SpeakCompleted += OnSpeakCompleted;
                _synthesizer.SpeakProgress += OnSpeakProgress;

                System.Diagnostics.Debug.WriteLine("Voice synthesis initialized successfully");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to initialize voice synthesis: {ex.Message}", ex);
            }
        }

        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

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
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to speak text: {ex.Message}", ex);
            }
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                _synthesizer.Speak(text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to speak text: {ex.Message}", ex);
            }
        }

        public void SpeakAsync(string text, bool waitForCompletion = false)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            try
            {
                if (waitForCompletion)
                {
                    _synthesizer.Speak(text);
                }
                else
                {
                    _synthesizer.SpeakAsync(text);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to speak text asynchronously: {ex.Message}", ex);
            }
        }

        public void StopSpeaking()
        {
            try
            {
                _synthesizer.SpeakAsyncCancelAll();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping speech: {ex.Message}");
            }
        }

        public void SetRate(int rate)
        {
            try
            {
                // Rate range is -10 to 10
                var clampedRate = Math.Max(-10, Math.Min(10, rate));
                _synthesizer.Rate = clampedRate;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set speech rate: {ex.Message}", ex);
            }
        }

        public void SetVolume(int volume)
        {
            try
            {
                // Volume range is 0 to 100
                var clampedVolume = Math.Max(0, Math.Min(100, volume));
                _synthesizer.Volume = clampedVolume;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set speech volume: {ex.Message}", ex);
            }
        }

        public void SetVoice(string voiceName)
        {
            try
            {
                var availableVoices = _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => v.VoiceInfo.Name)
                    .ToList();

                if (availableVoices.Contains(voiceName))
                {
                    _synthesizer.SelectVoice(voiceName);
                }
                else
                {
                    throw new ArgumentException($"Voice '{voiceName}' not found. Available voices: {string.Join(", ", availableVoices)}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set voice: {ex.Message}", ex);
            }
        }

        public string[] GetAvailableVoices()
        {
            try
            {
                return _synthesizer.GetInstalledVoices()
                    .Where(v => v.Enabled)
                    .Select(v => v.VoiceInfo.Name)
                    .ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get available voices: {ex.Message}", ex);
            }
        }

        public string GetCurrentVoice()
        {
            return _synthesizer.Voice.Name;
        }

        public int GetCurrentRate()
        {
            return _synthesizer.Rate;
        }

        public int GetCurrentVolume()
        {
            return _synthesizer.Volume;
        }

        public bool IsSpeaking()
        {
            return _synthesizer.State == SynthesizerState.Speaking;
        }

        // Predefined common responses
        public async Task SpeakWelcomeAsync()
        {
            await SpeakAsync("AION Desktop Assistant is ready to help you. You can use voice commands to control your computer.");
        }

        public async Task SpeakErrorAsync(string error)
        {
            await SpeakAsync($"Error: {error}");
        }

        public async Task SpeakConfirmationAsync(string action)
        {
            await SpeakAsync($"Action completed: {action}");
        }

        public async Task SpeakHelpAsync()
        {
            await SpeakAsync("Available commands include: read screen, click, type text, switch window, copy, paste, and many more. Say 'help me' for detailed assistance.");
        }

        public async Task SpeakScreenContentAsync(string content)
        {
            if (content.Length > 200)
            {
                content = content.Substring(0, 200) + "... and more";
            }
            await SpeakAsync($"Screen content: {content}");
        }

        public async Task SpeakNavigationAsync(string location)
        {
            await SpeakAsync($"Navigated to {location}");
        }

        public async Task SpeakTypingAsync(string text)
        {
            if (text.Length > 50)
            {
                await SpeakAsync($"Typed text starting with: {text.Substring(0, 50)}");
            }
            else
            {
                await SpeakAsync($"Typed: {text}");
            }
        }

        // Event handlers
        private void OnSpeakStarted(object? sender, SpeakStartedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Speech started");
        }

        private void OnSpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                System.Diagnostics.Debug.WriteLine($"Speech error: {e.Error.Message}");
            }
            else if (e.Cancelled)
            {
                System.Diagnostics.Debug.WriteLine("Speech cancelled");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Speech completed successfully");
            }
        }

        private void OnSpeakProgress(object? sender, SpeakProgressEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Speaking: {e.Text}");
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
                    StopSpeaking();
                    _synthesizer?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing voice synthesis: {ex.Message}");
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