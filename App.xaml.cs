using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using AionDesktopAssistant.Services;
using Serilog;

namespace AionDesktopAssistant
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName()
                .Enrich.WithThreadId()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}")
                .WriteTo.File("logs/aion-desktop-assistant-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} <{SourceContext}> [{ProcessId}:{ThreadId}]{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("🚀 AION Desktop Assistant starting up...");
                Log.Information("Application Version: {Version}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
                Log.Information("Operating System: {OS}", Environment.OSVersion);
                Log.Information("Machine Name: {MachineName}", Environment.MachineName);
                Log.Information("User: {UserName}", Environment.UserName);

                _host = Host.CreateDefaultBuilder()
                    .UseSerilog() // Use Serilog for logging
                    .ConfigureServices(ConfigureServices)
                    .Build();

                _host.Start();
                Log.Information("✅ Host services started successfully");

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                mainWindow.Show();
                Log.Information("✅ Main window displayed");

                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "❌ Application failed to start");
                MessageBox.Show($"Failed to start AION Desktop Assistant: {ex.Message}", "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private void ConfigureServices(IServiceCollection services)
        {
            Log.Information("🔧 Configuring dependency injection services...");

            // Register all services with logging
            services.AddSingleton(serviceProvider =>
            {
                Log.Information("📸 Initializing ScreenCaptureService");
                return new ScreenCaptureService();
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("👁️ Initializing OcrService");
                return new OcrService();
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("🎤 Initializing VoiceRecognitionService");
                return new VoiceRecognitionService();
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("🔊 Initializing VoiceSynthesisService");
                return new VoiceSynthesisService();
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("🖱️ Initializing MouseAutomationService");
                return new MouseAutomationService();
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("⌨️ Initializing KeyboardAutomationService");
                return new KeyboardAutomationService();
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("🪟 Initializing WindowManagementService");
                return new WindowManagementService();
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("♿ Initializing AccessibilityService");
                return new AccessibilityService();
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("🤖 Initializing ClaudeCodeIntegrationService");
                var voiceRecognition = serviceProvider.GetRequiredService<VoiceRecognitionService>();
                var voiceSynthesis = serviceProvider.GetRequiredService<VoiceSynthesisService>();
                var screenCapture = serviceProvider.GetRequiredService<ScreenCaptureService>();
                var ocr = serviceProvider.GetRequiredService<OcrService>();
                return new ClaudeCodeIntegrationService(voiceRecognition, voiceSynthesis, screenCapture, ocr);
            });

            services.AddSingleton(serviceProvider =>
            {
                Log.Information("🔄 Initializing AionRemoteControlService");
                var screenCapture = serviceProvider.GetRequiredService<ScreenCaptureService>();
                var ocr = serviceProvider.GetRequiredService<OcrService>();
                var voiceRecognition = serviceProvider.GetRequiredService<VoiceRecognitionService>();
                var voiceSynthesis = serviceProvider.GetRequiredService<VoiceSynthesisService>();
                var mouseAutomation = serviceProvider.GetRequiredService<MouseAutomationService>();
                var keyboardAutomation = serviceProvider.GetRequiredService<KeyboardAutomationService>();
                var windowManagement = serviceProvider.GetRequiredService<WindowManagementService>();
                var accessibility = serviceProvider.GetRequiredService<AccessibilityService>();
                return new AionRemoteControlService(screenCapture, ocr, voiceRecognition, voiceSynthesis,
                    mouseAutomation, keyboardAutomation, windowManagement, accessibility);
            });

            services.AddSingleton<MainWindow>();

            Log.Information("✅ All services registered in DI container");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("🛑 AION Desktop Assistant shutting down...");
            Log.Information("Exit code: {ExitCode}", e.ApplicationExitCode);

            try
            {
                _host?.Dispose();
                Log.Information("✅ Host disposed successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ Error during host disposal");
            }
            finally
            {
                Log.Information("👋 AION Desktop Assistant shutdown complete");
                Log.CloseAndFlush();
            }

            base.OnExit(e);
        }

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.Exception, "💥 Unhandled exception in UI thread");

            MessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nThe application will continue running, but please check the logs for details.",
                "Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            e.Handled = true; // Prevent application crash
        }

        static App()
        {
            // Set up global exception handlers
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "💥 Unhandled exception in application domain. Terminating: {IsTerminating}", e.IsTerminating);
                }
                else
                {
                    Log.Fatal("💥 Unhandled non-exception object: {ExceptionObject}. Terminating: {IsTerminating}",
                        e.ExceptionObject?.ToString() ?? "null", e.IsTerminating);
                }
            };
        }
    }
}