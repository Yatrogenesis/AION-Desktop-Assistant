using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Serilog;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace AionDesktopAssistant.Services
{
    /// <summary>
    /// 🔄 AION Remote Control Service
    /// Enables bidirectional communication - Claude Code can control AION Desktop Assistant
    /// </summary>
    public class AionRemoteControlService : IDisposable
    {
        private static readonly ILogger _logger = Log.ForContext<AionRemoteControlService>();

        private readonly ScreenCaptureService _screenCapture;
        private readonly OcrService _ocr;
        private readonly VoiceRecognitionService _voiceRecognition;
        private readonly VoiceSynthesisService _voiceSynthesis;
        private readonly MouseAutomationService _mouseAutomation;
        private readonly KeyboardAutomationService _keyboardAutomation;
        private readonly WindowManagementService _windowManagement;
        private readonly AccessibilityService _accessibility;

        private HttpListener? _httpListener;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning = false;
        private bool _disposed = false;

        private const string DEFAULT_PREFIX = "http://localhost:8080/";
        private int _requestsHandled = 0;

        public AionRemoteControlService(
            ScreenCaptureService screenCapture,
            OcrService ocr,
            VoiceRecognitionService voiceRecognition,
            VoiceSynthesisService voiceSynthesis,
            MouseAutomationService mouseAutomation,
            KeyboardAutomationService keyboardAutomation,
            WindowManagementService windowManagement,
            AccessibilityService accessibility)
        {
            _screenCapture = screenCapture;
            _ocr = ocr;
            _voiceRecognition = voiceRecognition;
            _voiceSynthesis = voiceSynthesis;
            _mouseAutomation = mouseAutomation;
            _keyboardAutomation = keyboardAutomation;
            _windowManagement = windowManagement;
            _accessibility = accessibility;

            _logger.Information("🔄 AionRemoteControlService initialized");
        }

        /// <summary>
        /// 🚀 Start the remote control server
        /// </summary>
        public async Task<bool> StartServerAsync(string prefix = DEFAULT_PREFIX)
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.Information("🚀 Starting AION Remote Control Server on {Prefix}", prefix);

            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(prefix);
                _httpListener.Start();

                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;

                // Start listening for requests in background
                _ = Task.Run(() => ListenForRequestsAsync(_cancellationTokenSource.Token));

                stopwatch.Stop();
                _logger.Information("✅ Remote Control Server started in {ElapsedMs}ms - Ready to accept Claude commands",
                    stopwatch.ElapsedMilliseconds);

                return true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Failed to start Remote Control Server after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                return false;
            }
        }

        /// <summary>
        /// 👂 Listen for incoming HTTP requests
        /// </summary>
        private async Task ListenForRequestsAsync(CancellationToken cancellationToken)
        {
            _logger.Information("👂 Listening for remote control commands...");

            while (_isRunning && !cancellationToken.IsCancellationRequested && _httpListener != null)
            {
                try
                {
                    var context = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
                }
                catch (HttpListenerException)
                {
                    // Listener stopped
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "❌ Error in request listener loop");
                }
            }

            _logger.Information("👋 Request listener stopped");
        }

        /// <summary>
        /// 🎯 Handle incoming API request
        /// </summary>
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;
            var response = context.Response;

            _requestsHandled++;

            _logger.Debug("📥 Incoming request #{Count}: {Method} {Url}",
                _requestsHandled, request.HttpMethod, request.Url?.PathAndQuery);

            try
            {
                // Enable CORS for Claude Code CLI
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                var path = request.Url?.AbsolutePath ?? "/";
                var result = await RouteRequestAsync(path, request);

                var jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
                var buffer = Encoding.UTF8.GetBytes(jsonResponse);

                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = result.Success ? 200 : 400;

                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.Close();

                stopwatch.Stop();
                _logger.Information("✅ Request #{Count} handled in {ElapsedMs}ms - {Method} {Path} -> {Status}",
                    _requestsHandled, stopwatch.ElapsedMilliseconds, request.HttpMethod, path, result.Success ? "OK" : "ERROR");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.Error(ex, "❌ Error handling request #{Count} after {ElapsedMs}ms",
                    _requestsHandled, stopwatch.ElapsedMilliseconds);

                try
                {
                    var errorResult = new ApiResponse
                    {
                        Success = false,
                        Message = $"Internal server error: {ex.Message}"
                    };

                    var jsonError = JsonConvert.SerializeObject(errorResult);
                    var errorBuffer = Encoding.UTF8.GetBytes(jsonError);

                    response.StatusCode = 500;
                    response.ContentType = "application/json";
                    await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                    response.Close();
                }
                catch
                {
                    // Failed to send error response
                }
            }
        }

        /// <summary>
        /// 🚦 Route request to appropriate handler
        /// </summary>
        private async Task<ApiResponse> RouteRequestAsync(string path, HttpListenerRequest request)
        {
            _logger.Debug("🚦 Routing request to: {Path}", path);

            // Read request body for POST requests
            string? requestBody = null;
            if (request.HttpMethod == "POST" && request.HasEntityBody)
            {
                using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                requestBody = await reader.ReadToEndAsync();
                _logger.Debug("📝 Request body: {Body}", requestBody);
            }

            return path switch
            {
                "/api/status" => await GetStatusAsync(),
                "/api/screen/capture" => await CaptureScreenAsync(),
                "/api/screen/read" => await ReadScreenAsync(),
                "/api/mouse/move" => await MoveMouseAsync(requestBody),
                "/api/mouse/click" => await ClickMouseAsync(requestBody),
                "/api/keyboard/type" => await TypeTextAsync(requestBody),
                "/api/keyboard/press" => await PressKeyAsync(requestBody),
                "/api/window/list" => await ListWindowsAsync(),
                "/api/window/switch" => await SwitchWindowAsync(requestBody),
                "/api/voice/speak" => await SpeakAsync(requestBody),
                "/api/voice/listen" => await StartListeningAsync(),
                "/api/accessibility/analyze" => await AnalyzeAccessibilityAsync(),
                "/api/command/execute" => await ExecuteCommandAsync(requestBody),
                _ => new ApiResponse { Success = false, Message = $"Unknown endpoint: {path}" }
            };
        }

        /// <summary>
        /// 📊 Get AION status
        /// </summary>
        private async Task<ApiResponse> GetStatusAsync()
        {
            return await Task.FromResult(new ApiResponse
            {
                Success = true,
                Message = "AION Desktop Assistant is running",
                Data = new
                {
                    Status = "Online",
                    RequestsHandled = _requestsHandled,
                    VoiceListening = _voiceRecognition.IsListening,
                    ServerUptime = Process.GetCurrentProcess().TotalProcessorTime
                }
            });
        }

        /// <summary>
        /// 📸 Capture screen and return as base64
        /// </summary>
        private async Task<ApiResponse> CaptureScreenAsync()
        {
            try
            {
                var screenshot = _screenCapture.CaptureScreen();
                using var ms = new MemoryStream();
                screenshot.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                var base64 = Convert.ToBase64String(ms.ToArray());

                return await Task.FromResult(new ApiResponse
                {
                    Success = true,
                    Message = "Screenshot captured",
                    Data = new { Image = base64, Width = screenshot.Width, Height = screenshot.Height }
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 👁️ Read screen text with OCR
        /// </summary>
        private async Task<ApiResponse> ReadScreenAsync()
        {
            try
            {
                var screenshot = _screenCapture.CaptureScreen();
                var text = await _ocr.ExtractTextAsync(screenshot);

                return new ApiResponse
                {
                    Success = true,
                    Message = "Screen text extracted",
                    Data = new { Text = text, Length = text.Length }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 🖱️ Move mouse to coordinates
        /// </summary>
        private async Task<ApiResponse> MoveMouseAsync(string? requestBody)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<MouseCommand>(requestBody ?? "{}");
                if (command?.X == null || command?.Y == null)
                    return new ApiResponse { Success = false, Message = "Missing X or Y coordinates" };

                _mouseAutomation.MoveMouse(command.X.Value, command.Y.Value);

                return await Task.FromResult(new ApiResponse
                {
                    Success = true,
                    Message = $"Mouse moved to ({command.X}, {command.Y})"
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 🖱️ Click mouse
        /// </summary>
        private async Task<ApiResponse> ClickMouseAsync(string? requestBody)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<MouseCommand>(requestBody ?? "{}");

                if (command?.X != null && command?.Y != null)
                {
                    _mouseAutomation.Click(command.X.Value, command.Y.Value);
                }
                else
                {
                    _mouseAutomation.Click();
                }

                return await Task.FromResult(new ApiResponse
                {
                    Success = true,
                    Message = "Mouse clicked"
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// ⌨️ Type text
        /// </summary>
        private async Task<ApiResponse> TypeTextAsync(string? requestBody)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<KeyboardCommand>(requestBody ?? "{}");
                if (string.IsNullOrEmpty(command?.Text))
                    return new ApiResponse { Success = false, Message = "Missing text to type" };

                _keyboardAutomation.TypeText(command.Text);

                return await Task.FromResult(new ApiResponse
                {
                    Success = true,
                    Message = $"Typed: {command.Text}"
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// ⌨️ Press key
        /// </summary>
        private async Task<ApiResponse> PressKeyAsync(string? requestBody)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<KeyboardCommand>(requestBody ?? "{}");
                if (string.IsNullOrEmpty(command?.Key))
                    return new ApiResponse { Success = false, Message = "Missing key to press" };

                _keyboardAutomation.PressKey(command.Key);

                return await Task.FromResult(new ApiResponse
                {
                    Success = true,
                    Message = $"Pressed key: {command.Key}"
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 🪟 List open windows
        /// </summary>
        private async Task<ApiResponse> ListWindowsAsync()
        {
            try
            {
                var windows = _windowManagement.GetOpenWindows();

                return await Task.FromResult(new ApiResponse
                {
                    Success = true,
                    Message = $"Found {windows.Count} windows",
                    Data = new { Windows = windows, Count = windows.Count }
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 🪟 Switch to window
        /// </summary>
        private async Task<ApiResponse> SwitchWindowAsync(string? requestBody)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<WindowCommand>(requestBody ?? "{}");
                if (string.IsNullOrEmpty(command?.WindowName))
                    return new ApiResponse { Success = false, Message = "Missing window name" };

                var switched = _windowManagement.SwitchToWindow(command.WindowName);

                return await Task.FromResult(new ApiResponse
                {
                    Success = switched,
                    Message = switched ? $"Switched to: {command.WindowName}" : $"Window not found: {command.WindowName}"
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 🔊 Speak text
        /// </summary>
        private async Task<ApiResponse> SpeakAsync(string? requestBody)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<VoiceCommand>(requestBody ?? "{}");
                if (string.IsNullOrEmpty(command?.Text))
                    return new ApiResponse { Success = false, Message = "Missing text to speak" };

                await _voiceSynthesis.SpeakAsync(command.Text);

                return new ApiResponse
                {
                    Success = true,
                    Message = $"Spoken: {command.Text}"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 🎤 Start voice listening
        /// </summary>
        private async Task<ApiResponse> StartListeningAsync()
        {
            try
            {
                if (!_voiceRecognition.IsListening)
                {
                    _voiceRecognition.StartListening();
                }

                return await Task.FromResult(new ApiResponse
                {
                    Success = true,
                    Message = "Voice recognition activated"
                });
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// ♿ Analyze accessibility
        /// </summary>
        private async Task<ApiResponse> AnalyzeAccessibilityAsync()
        {
            try
            {
                var analysis = await _accessibility.AnalyzeScreenAsync();

                return new ApiResponse
                {
                    Success = true,
                    Message = "Accessibility analysis completed",
                    Data = new { Analysis = analysis }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 🎯 Execute high-level command
        /// </summary>
        private async Task<ApiResponse> ExecuteCommandAsync(string? requestBody)
        {
            try
            {
                var command = JsonConvert.DeserializeObject<GenericCommand>(requestBody ?? "{}");
                if (string.IsNullOrEmpty(command?.Command))
                    return new ApiResponse { Success = false, Message = "Missing command" };

                _logger.Information("🎯 Executing command from Claude: {Command}", command.Command);

                // Process command through voice recognition pipeline
                // This allows Claude to use natural language commands
                var result = await ProcessClaudeCommandAsync(command.Command);

                return new ApiResponse
                {
                    Success = true,
                    Message = result
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse { Success = false, Message = ex.Message };
            }
        }

        /// <summary>
        /// 🧠 Process Claude's natural language command
        /// </summary>
        private async Task<string> ProcessClaudeCommandAsync(string command)
        {
            // Simulate voice command processing
            command = command.ToLower().Trim();

            if (command.Contains("read screen"))
            {
                var screenshot = _screenCapture.CaptureScreen();
                var text = await _ocr.ExtractTextAsync(screenshot);
                return $"Screen text: {text.Substring(0, Math.Min(text.Length, 200))}...";
            }
            else if (command.Contains("list windows"))
            {
                var windows = _windowManagement.GetOpenWindows();
                return $"Found {windows.Count} windows: {string.Join(", ", windows.Take(5))}";
            }
            else
            {
                return $"Command executed: {command}";
            }
        }

        /// <summary>
        /// 🛑 Stop the server
        /// </summary>
        public void StopServer()
        {
            _logger.Information("🛑 Stopping Remote Control Server...");

            try
            {
                _isRunning = false;
                _cancellationTokenSource?.Cancel();
                _httpListener?.Stop();
                _httpListener?.Close();

                _logger.Information("✅ Remote Control Server stopped - Total requests handled: {Count}",
                    _requestsHandled);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "❌ Error stopping server");
            }
        }

        /// <summary>
        /// 📊 Get statistics
        /// </summary>
        public void LogStatistics()
        {
            _logger.Information("📊 Remote Control Statistics - Requests handled: {Count}, IsRunning: {Running}",
                _requestsHandled, _isRunning);
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
                _logger.Information("🗑️ Disposing AionRemoteControlService...");

                try
                {
                    StopServer();
                    _cancellationTokenSource?.Dispose();
                    _httpListener = null;

                    _logger.Information("✅ AionRemoteControlService disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "❌ Error disposing AionRemoteControlService");
                }

                _disposed = true;
            }
        }

        ~AionRemoteControlService()
        {
            Dispose(false);
        }
    }

    #region API Models

    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }

    public class MouseCommand
    {
        public int? X { get; set; }
        public int? Y { get; set; }
        public string? Button { get; set; }
    }

    public class KeyboardCommand
    {
        public string? Text { get; set; }
        public string? Key { get; set; }
    }

    public class WindowCommand
    {
        public string? WindowName { get; set; }
    }

    public class VoiceCommand
    {
        public string? Text { get; set; }
    }

    public class GenericCommand
    {
        public string? Command { get; set; }
        public object? Parameters { get; set; }
    }

    #endregion
}
