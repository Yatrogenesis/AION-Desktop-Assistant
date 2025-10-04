using Xunit;
using Moq;
using AionDesktopAssistant.Services;
using System.Threading.Tasks;
using System.Drawing;

namespace AionDesktopAssistant.Tests
{
    /// <summary>
    /// 🤖 Claude Integration Service Tests
    /// </summary>
    public class ClaudeIntegrationServiceTests
    {
        private readonly Mock<VoiceRecognitionService> _mockVoiceRecognition;
        private readonly Mock<VoiceSynthesisService> _mockVoiceSynthesis;
        private readonly Mock<ScreenCaptureService> _mockScreenCapture;
        private readonly Mock<OcrService> _mockOcr;
        private readonly ClaudeCodeIntegrationService _service;

        public ClaudeIntegrationServiceTests()
        {
            _mockVoiceRecognition = new Mock<VoiceRecognitionService>();
            _mockVoiceSynthesis = new Mock<VoiceSynthesisService>();
            _mockScreenCapture = new Mock<ScreenCaptureService>();
            _mockOcr = new Mock<OcrService>();

            _service = new ClaudeCodeIntegrationService(
                _mockVoiceRecognition.Object,
                _mockVoiceSynthesis.Object,
                _mockScreenCapture.Object,
                _mockOcr.Object
            );
        }

        [Fact]
        public async Task InitializeAsync_ShouldCheckForClaudeCodeCLI()
        {
            // Act
            var result = await _service.InitializeAsync();

            // Assert
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task AskClaudeAsync_ShouldProcessQuestion()
        {
            // Arrange
            var question = "How do I close this window?";
            _mockScreenCapture.Setup(s => s.CaptureScreen())
                .Returns(new Bitmap(100, 100));
            _mockOcr.Setup(o => o.ExtractTextAsync(It.IsAny<Bitmap>()))
                .ReturnsAsync("Sample screen text");

            // Act
            var result = await _service.AskClaudeAsync(question);

            // Assert
            Assert.NotNull(result);
            _mockScreenCapture.Verify(s => s.CaptureScreen(), Times.Once);
            _mockOcr.Verify(o => o.ExtractTextAsync(It.IsAny<Bitmap>()), Times.Once);
        }

        [Fact]
        public async Task SendVoiceCommandToClaudeAsync_ShouldCaptureScreenContext()
        {
            // Arrange
            var command = "analyze this screen";
            _mockScreenCapture.Setup(s => s.CaptureScreen())
                .Returns(new Bitmap(100, 100));
            _mockOcr.Setup(o => o.ExtractTextAsync(It.IsAny<Bitmap>()))
                .ReturnsAsync("Test screen content");

            // Act
            var result = await _service.SendVoiceCommandToClaudeAsync(command);

            // Assert
            Assert.NotNull(result);
            _mockScreenCapture.Verify(s => s.CaptureScreen(), Times.Once);
        }

        [Fact]
        public async Task AnalyzeScreenWithClaudeAsync_ShouldExtractScreenText()
        {
            // Arrange
            var bitmap = new Bitmap(100, 100);
            _mockScreenCapture.Setup(s => s.CaptureScreen()).Returns(bitmap);
            _mockOcr.Setup(o => o.ExtractTextAsync(bitmap))
                .ReturnsAsync("Screen analysis text");

            // Act
            var result = await _service.AnalyzeScreenWithClaudeAsync();

            // Assert
            Assert.NotNull(result);
            _mockOcr.Verify(o => o.ExtractTextAsync(bitmap), Times.Once);
        }

        [Fact]
        public void LogStatistics_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => _service.LogStatistics());
            Assert.Null(exception);
        }
    }
}
