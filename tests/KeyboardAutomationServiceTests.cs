using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;
using System.Windows.Forms;

namespace AionDesktopAssistant.Tests
{
    public class KeyboardAutomationServiceTests
    {
        private readonly KeyboardAutomationService _sut;

        public KeyboardAutomationServiceTests()
        {
            _sut = new KeyboardAutomationService();
        }

        [Fact]
        public void SendText_WithValidText_ShouldNotThrow()
        {
            // Arrange
            var text = "Hello World";

            // Act
            Action act = () => _sut.SendText(text);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SendText_WithNullText_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => _sut.SendText(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SendKey_WithValidKey_ShouldNotThrow()
        {
            // Arrange
            var key = Keys.Enter;

            // Act
            Action act = () => _sut.SendKey(key);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SendKeyDown_WithValidKey_ShouldNotThrow()
        {
            // Arrange
            var key = Keys.Shift;

            // Act
            Action act = () => _sut.SendKeyDown(key);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SendKeyUp_WithValidKey_ShouldNotThrow()
        {
            // Arrange
            var key = Keys.Shift;

            // Act
            Action act = () => _sut.SendKeyUp(key);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SendKeyCombo_WithValidKeys_ShouldNotThrow()
        {
            // Arrange
            var keys = new Keys[] { Keys.Control, Keys.C };

            // Act
            Action act = () => _sut.SendKeyCombo(keys);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SendKeyCombo_WithNullKeys_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => _sut.SendKeyCombo(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SendKeyCombo_WithEmptyKeys_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => _sut.SendKeyCombo(new Keys[0]);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TypeWithDelay_WithValidParameters_ShouldNotThrow()
        {
            // Arrange
            var text = "Test";
            var delayMs = 50;

            // Act
            Action act = () => _sut.TypeWithDelay(text, delayMs);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void TypeWithDelay_WithNegativeDelay_ShouldThrowArgumentException()
        {
            // Arrange
            var text = "Test";
            var delayMs = -1;

            // Act
            Action act = () => _sut.TypeWithDelay(text, delayMs);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void IsKeyPressed_WithValidKey_ShouldReturnBool()
        {
            // Arrange
            var key = Keys.CapsLock;

            // Act
            var result = _sut.IsKeyPressed(key);

            // Assert
            result.Should().BeOfType<bool>();
        }

        [Fact]
        public void SendAltTab_ShouldNotThrow()
        {
            // Act
            Action act = () => _sut.SendAltTab();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SendCtrlShortcut_WithValidKey_ShouldNotThrow()
        {
            // Arrange
            var key = Keys.S;

            // Act
            Action act = () => _sut.SendCtrlShortcut(key);

            // Assert
            act.Should().NotThrow();
        }
    }
}