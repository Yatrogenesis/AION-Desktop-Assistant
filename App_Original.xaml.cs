using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using AionDesktopAssistant.Services;

namespace AionDesktopAssistant
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(ConfigureServices)
                .Build();

            _host.Start();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ScreenCaptureService>();
            services.AddSingleton<OcrService>();
            services.AddSingleton<VoiceRecognitionService>();
            services.AddSingleton<VoiceSynthesisService>();
            services.AddSingleton<MouseAutomationService>();
            services.AddSingleton<KeyboardAutomationService>();
            services.AddSingleton<WindowManagementService>();
            services.AddSingleton<AccessibilityService>();
            services.AddSingleton<MainWindow>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}