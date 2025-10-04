using Xunit;
using Moq;
using AionDesktopAssistant.Services;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace AionDesktopAssistant.Tests
{
    /// <summary>
    /// 🧪 Remote Control Service Tests
    /// </summary>
    public class RemoteControlServiceTests : IDisposable
    {
        private readonly Mock<ScreenCaptureService> _mockScreenCapture;
        private readonly Mock<OcrService> _mockOcr;
        private readonly Mock<VoiceRecognitionService> _mockVoiceRecognition;
        private readonly Mock<VoiceSynthesisService> _mockVoiceSynthesis;
        private readonly Mock<MouseAutomationService> _mockMouseAutomation;
        private readonly Mock<KeyboardAutomationService> _mockKeyboardAutomation;
        private readonly Mock<WindowManagementService> _mockWindowManagement;
        private readonly Mock<AccessibilityService> _mockAccessibility;

        private readonly AionRemoteControlService _service;
        private readonly HttpClient _httpClient;

        public RemoteControlServiceTests()
        {
            _mockScreenCapture = new Mock<ScreenCaptureService>();
            _mockOcr = new Mock<OcrService>();
            _mockVoiceRecognition = new Mock<VoiceRecognitionService>();
            _mockVoiceSynthesis = new Mock<VoiceSynthesisService>();
            _mockMouseAutomation = new Mock<MouseAutomationService>();
            _mockKeyboardAutomation = new Mock<KeyboardAutomationService>();
            _mockWindowManagement = new Mock<WindowManagementService>();
            _mockAccessibility = new Mock<AccessibilityService>();

            _service = new AionRemoteControlService(
                _mockScreenCapture.Object,
                _mockOcr.Object,
                _mockVoiceRecognition.Object,
                _mockVoiceSynthesis.Object,
                _mockMouseAutomation.Object,
                _mockKeyboardAutomation.Object,
                _mockWindowManagement.Object,
                _mockAccessibility.Object
            );

            _httpClient = new HttpClient();
        }

        [Fact]
        public async Task StartServer_ShouldReturnTrue_WhenSuccessful()
        {
            // Arrange & Act
            var result = await _service.StartServerAsync("http://localhost:8081/");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task StatusEndpoint_ShouldReturnOnlineStatus()
        {
            // Arrange
            await _service.StartServerAsync("http://localhost:8082/");

            // Act
            var response = await _httpClient.GetAsync("http://localhost:8082/api/status");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(content);

            // Assert
            Assert.True(result.Success);
            Assert.Contains("running", result.Message.ToLower());
        }

        [Fact]
        public async Task MouseMoveEndpoint_ShouldCallMouseAutomation()
        {
            // Arrange
            await _service.StartServerAsync("http://localhost:8083/");
            var command = new { X = 100, Y = 200 };
            var json = JsonConvert.SerializeObject(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("http://localhost:8083/api/mouse/move", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseContent);

            // Assert
            Assert.True(result.Success);
            _mockMouseAutomation.Verify(m => m.MoveMouse(100, 200), Times.Once);
        }

        [Fact]
        public async Task TypeTextEndpoint_ShouldCallKeyboardAutomation()
        {
            // Arrange
            await _service.StartServerAsync("http://localhost:8084/");
            var command = new { Text = "Hello World" };
            var json = JsonConvert.SerializeObject(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("http://localhost:8084/api/keyboard/type", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseContent);

            // Assert
            Assert.True(result.Success);
            _mockKeyboardAutomation.Verify(k => k.TypeText("Hello World"), Times.Once);
        }

        [Fact]
        public async Task SpeakEndpoint_ShouldCallVoiceSynthesis()
        {
            // Arrange
            await _service.StartServerAsync("http://localhost:8085/");
            var command = new { Text = "Test speech" };
            var json = JsonConvert.SerializeObject(command);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _httpClient.PostAsync("http://localhost:8085/api/voice/speak", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(responseContent);

            // Assert
            Assert.True(result.Success);
            _mockVoiceSynthesis.Verify(v => v.SpeakAsync("Test speech"), Times.Once);
        }

        [Fact]
        public async Task UnknownEndpoint_ShouldReturnNotFound()
        {
            // Arrange
            await _service.StartServerAsync("http://localhost:8086/");

            // Act
            var response = await _httpClient.GetAsync("http://localhost:8086/api/unknown");
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ApiResponse>(content);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Unknown endpoint", result.Message);
        }

        public void Dispose()
        {
            _service?.StopServer();
            _service?.Dispose();
            _httpClient?.Dispose();
        }
    }
}
