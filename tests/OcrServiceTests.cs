using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;
using System.Drawing;

namespace AionDesktopAssistant.Tests
{
    public class OcrServiceTests
    {
        private readonly OcrService _sut;

        public OcrServiceTests()
        {
            _sut = new OcrService();
        }

        [Fact]
        public void ExtractText_WithValidImage_ReturnsText()
        {
            // Arrange
            using var testImage = new Bitmap(100, 100);
            using var graphics = Graphics.FromImage(testImage);
            graphics.Clear(Color.White);

            // Act
            var result = _sut.ExtractText(testImage);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void ExtractText_WithNullImage_ThrowsArgumentNullException()
        {
            // Act
            Action act = () => _sut.ExtractText(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Initialize_ShouldConfigureTesseractCorrectly()
        {
            // Act
            _sut.Initialize();

            // Assert
            _sut.IsInitialized.Should().BeTrue();
        }
    }
}
