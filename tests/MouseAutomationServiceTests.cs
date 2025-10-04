using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;
using System.Drawing;

namespace AionDesktopAssistant.Tests
{
    public class MouseAutomationServiceTests
    {
        private readonly MouseAutomationService _sut;

        public MouseAutomationServiceTests()
        {
            _sut = new MouseAutomationService();
        }

        [Fact]
        public void MoveTo_WithValidCoordinates_ShouldNotThrow()
        {
            // Arrange
            var x = 100;
            var y = 100;

            // Act
            Action act = () => _sut.MoveTo(x, y);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void MoveTo_WithNegativeCoordinates_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => _sut.MoveTo(-1, -1);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Click_WithValidCoordinates_ShouldNotThrow()
        {
            // Arrange
            var x = 100;
            var y = 100;

            // Act
            Action act = () => _sut.Click(x, y);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void DoubleClick_WithValidCoordinates_ShouldNotThrow()
        {
            // Arrange
            var x = 100;
            var y = 100;

            // Act
            Action act = () => _sut.DoubleClick(x, y);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void RightClick_WithValidCoordinates_ShouldNotThrow()
        {
            // Arrange
            var x = 100;
            var y = 100;

            // Act
            Action act = () => _sut.RightClick(x, y);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Drag_WithValidCoordinates_ShouldNotThrow()
        {
            // Arrange
            var startX = 100;
            var startY = 100;
            var endX = 200;
            var endY = 200;

            // Act
            Action act = () => _sut.Drag(startX, startY, endX, endY);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Scroll_WithValidDirection_ShouldNotThrow()
        {
            // Arrange
            var x = 100;
            var y = 100;
            var delta = 3;

            // Act
            Action act = () => _sut.Scroll(x, y, delta);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void GetCurrentPosition_ShouldReturnValidPoint()
        {
            // Act
            var position = _sut.GetCurrentPosition();

            // Assert
            position.X.Should().BeGreaterOrEqualTo(0);
            position.Y.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void SetCursorPosition_WithValidCoordinates_ShouldUpdatePosition()
        {
            // Arrange
            var targetX = 150;
            var targetY = 150;

            // Act
            _sut.SetCursorPosition(targetX, targetY);
            var currentPosition = _sut.GetCurrentPosition();

            // Assert
            currentPosition.X.Should().Be(targetX);
            currentPosition.Y.Should().Be(targetY);
        }

        [Fact]
        public void SmoothMoveTo_WithValidCoordinates_ShouldNotThrow()
        {
            // Arrange
            var x = 200;
            var y = 200;
            var duration = 100;

            // Act
            Action act = () => _sut.SmoothMoveTo(x, y, duration);

            // Assert
            act.Should().NotThrow();
        }
    }
}