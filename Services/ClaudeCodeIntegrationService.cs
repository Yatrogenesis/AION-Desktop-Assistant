using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using Serilog;
using System.Collections.Generic;

namespace AionDesktopAssistant.Services
{
    /// <summary>
    /// 🤖 Claude Code Integration Service
    /// Integrates AION Desktop Assistant with Claude Code CLI for AI-powered assistance
    /// </summary>
    public class ClaudeCodeIntegrationService : IDisposable
    {
        private static readonly ILogger _logger = Log.ForContext<ClaudeCodeIntegrationService>();
        private readonly VoiceRecognitionService _voiceRecognition;
        private readonly VoiceSynthesisService _voiceSynthesis;
        private readonly ScreenCaptureService _screenCapture;
        private readonly OcrService _ocr;

        private bool _disposed = false;
        private string _claudeCodePath = "claude-code";
        private int _interactionCount = 0;

        public ClaudeCodeIntegrationService(
            VoiceRecognitionService voiceRecognition,
            VoiceSynthesisService voiceSynthesis,
            ScreenCaptureService screenCapture,
            OcrService ocr)
        {
            _voiceRecognition = voiceRecognition;
            _voiceSynthesis = voiceSynthesis;
            _screenCapture = screenCapture;
            _ocr = ocr;

            _logger.Information("🤖 ClaudeCodeIntegrationService initialized");
        }

        /// <summary>
        /// 🔍 Initialize Claude Code CLI integration
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("🚀 Initializing Claude Code integration...");

            try
            {
                // Check if claude-code is available
                var available = await IsClaudeCodeAvailableAsync();

                if (!available)
                {
                    _logger.Warning("⚠️ Claude Code CLI not found in PATH");
                    _logger.Information("💡 Install Claude Code from: npm install -g @anthropic-ai/claude-dev");
                    return false;
                }

                stopwatch.Stop();
                _logger.Information("✅ Claude Code integration initialized successfully in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to initialize Claude Code integration after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                return false;
            }
        }

        /// <summary>
        /// 🔍 Check if Claude Code CLI is available
        /// </summary>
        private async Task<bool> IsClaudeCodeAvailableAsync()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c where claude-code",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    _claudeCodePath = output.Trim().Split('\n')[0].Trim();
                    _logger.Debug("📍 Claude Code found at: {Path}", _claudeCodePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "❌ Error checking Claude Code availability");
                return false;
            }
        }

        /// <summary>
        /// 🎤 Send voice command to Claude Code
        /// </summary>
        public async Task<string> SendVoiceCommandToClaudeAsync(string voiceCommand)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("🎤 Processing voice command for Claude: '{Command}'", voiceCommand);

            try
            {
                // Capture current screen context
                var screenshot = _screenCapture.CaptureScreen();
                var screenContext = await _ocr.ExtractTextAsync(screenshot);

                // Build enhanced prompt with context
                var enhancedPrompt = BuildContextualPrompt(voiceCommand, screenContext);

                // Execute Claude Code command
                var response = await ExecuteClaudeCodeAsync(enhancedPrompt);

                _interactionCount++;
                stopwatch.Stop();

                _logger.Information("✅ Claude response received in {ElapsedMs}ms - Interaction #{Count}",
                    stopwatch.ElapsedMilliseconds, _interactionCount);

                // Speak response back to user
                await _voiceSynthesis.SpeakAsync(response);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to process Claude command after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                var errorMessage = "Sorry, I couldn't process your request with Claude";
                await _voiceSynthesis.SpeakAsync(errorMessage);

                return errorMessage;
            }
        }

        /// <summary>
        /// 🧠 Build contextual prompt with screen information
        /// </summary>
        private string BuildContextualPrompt(string userCommand, string screenContext)
        {
            var prompt = $@"🤖 AION Desktop Assistant - Claude Integration

👤 User Voice Command: {userCommand}

📺 Current Screen Context:
{(!string.IsNullOrWhiteSpace(screenContext) ? screenContext.Substring(0, Math.Min(screenContext.Length, 500)) : "No text detected on screen")}

🎯 Task: Provide a concise, actionable response to help the user accomplish their goal.
Consider the screen context and provide step-by-step guidance if needed.";

            return prompt;
        }

        /// <summary>
        /// 🚀 Execute Claude Code CLI command
        /// </summary>
        private async Task<string> ExecuteClaudeCodeAsync(string prompt)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Debug("🔧 Executing Claude Code CLI...");

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c echo {EscapeArgument(prompt)} | claude-code",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };

                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                stopwatch.Stop();

                if (process.ExitCode == 0)
                {
                    _logger.Information("✅ Claude Code executed successfully in {ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return output.Trim();
                }
                else
                {
                    _logger.Error("❌ Claude Code execution failed - Exit code: {ExitCode}, Error: {Error}",
                        process.ExitCode, error);
                    return "Claude Code encountered an error. Please check your API configuration.";
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Exception executing Claude Code after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// 💬 Ask Claude a question with full context
        /// </summary>
        public async Task<string> AskClaudeAsync(string question)
        {
            _logger.Information("💬 Asking Claude: '{Question}'", question);
            return await SendVoiceCommandToClaudeAsync(question);
        }

        /// <summary>
        /// 🔍 Analyze screen and get Claude's interpretation
        /// </summary>
        public async Task<string> AnalyzeScreenWithClaudeAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("🔍 Requesting Claude screen analysis...");

            try
            {
                var screenshot = _screenCapture.CaptureScreen();
                var screenText = await _ocr.ExtractTextAsync(screenshot);

                var analysisPrompt = $@"🖥️ Screen Analysis Request

📋 Text detected on screen:
{screenText}

🎯 Please analyze what's on the screen and explain:
1. What application or window is active
2. What the user might be trying to do
3. Suggested next steps or actions

Keep the response concise and actionable.";

                var response = await ExecuteClaudeCodeAsync(analysisPrompt);

                stopwatch.Stop();
                _logger.Information("✅ Screen analysis completed in {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Screen analysis failed after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        /// <summary>
        /// 🎯 Get accessibility suggestions from Claude
        /// </summary>
        public async Task<string> GetAccessibilitySuggestionsAsync()
        {
            _logger.Information("🎯 Requesting accessibility suggestions from Claude...");

            var prompt = @"🔍 Accessibility Analysis Request

I'm using AION Desktop Assistant for accessibility support.
Please suggest voice commands and automation workflows that could help users with:
1. Quadriplegia (hands-free operation)
2. Visual impairment (screen reading)
3. Motor impairment (reduced physical interaction)

Provide 5 specific, actionable suggestions.";

            return await ExecuteClaudeCodeAsync(prompt);
        }

        /// <summary>
        /// 🛠️ Escape command line arguments
        /// </summary>
        private string EscapeArgument(string argument)
        {
            return argument
                .Replace("\"", "\\\"")
                .Replace("|", "^|")
                .Replace("&", "^&")
                .Replace("<", "^<")
                .Replace(">", "^>")
                .Replace("^", "^^");
        }

        /// <summary>
        /// 📊 Get integration statistics
        /// </summary>
        public void LogStatistics()
        {
            _logger.Information("📊 Claude Code Integration Statistics - Total Interactions: {Count}",
                _interactionCount);
        }

        /// <summary>
        /// 🧹 Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _logger.Information("🗑️ Disposing ClaudeCodeIntegrationService...");

                try
                {
                    LogStatistics();
                    _logger.Information("✅ ClaudeCodeIntegrationService disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "❌ Error disposing ClaudeCodeIntegrationService");
                }

                _disposed = true;
            }
        }

        ~ClaudeCodeIntegrationService()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// 🎤 Claude voice command extensions
    /// </summary>
    public static class ClaudeVoiceCommands
    {
        public static readonly Dictionary<string, string> Commands = new()
        {
            { "ask claude", "💬 Ask Claude a question" },
            { "claude help", "🤖 Get help from Claude" },
            { "analyze screen with claude", "🔍 Analyze screen with AI" },
            { "claude accessibility", "♿ Get accessibility suggestions" },
            { "claude explain", "📖 Explain what's on screen" },
            { "claude suggest", "💡 Get AI suggestions" },
            { "claude automate", "🤖 Automate current task" },
            { "claude optimize", "⚡ Optimize workflow" }
        };
    }
}
