using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;
using System.Drawing;

namespace AionDesktopAssistant.Tests
{
    public class ScreenCaptureServiceTests
    {
        private readonly ScreenCaptureService _sut;

        public ScreenCaptureServiceTests()
        {
            _sut = new ScreenCaptureService();
        }

        [Fact]
        public void CaptureScreen_ReturnsValidBitmap()
        {
            // Act
            var result = _sut.CaptureScreen();

            // Assert
            result.Should().NotBeNull();
            result.Width.Should().BeGreaterThan(0);
            result.Height.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CaptureWindow_WithInvalidHandle_ReturnsNull()
        {
            // Arrange
            var invalidHandle = IntPtr.Zero;

            // Act
            var result = _sut.CaptureWindow(invalidHandle);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void GetScreenBounds_ReturnsValidRectangle()
        {
            // Act
            var result = _sut.GetScreenBounds();

            // Assert
            result.Width.Should().BeGreaterThan(0);
            result.Height.Should().BeGreaterThan(0);
            result.X.Should().BeGreaterOrEqualTo(0);
            result.Y.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void CaptureRegion_WithValidRectangle_ReturnsValidBitmap()
        {
            // Arrange
            var region = new Rectangle(0, 0, 100, 100);

            // Act
            var result = _sut.CaptureRegion(region);

            // Assert
            result.Should().NotBeNull();
            result.Width.Should().Be(100);
            result.Height.Should().Be(100);
        }

        [Fact]
        public void CaptureRegion_WithInvalidRectangle_ThrowsArgumentException()
        {
            // Arrange
            var invalidRegion = new Rectangle(-1, -1, -100, -100);

            // Act
            Action act = () => _sut.CaptureRegion(invalidRegion);

            // Assert
            act.Should().Throw<ArgumentException>();
        }
    }
}