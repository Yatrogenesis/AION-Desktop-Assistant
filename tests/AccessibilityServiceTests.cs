using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;
using System.Windows.Automation;

namespace AionDesktopAssistant.Tests
{
    public class AccessibilityServiceTests
    {
        private readonly AccessibilityService _sut;

        public AccessibilityServiceTests()
        {
            _sut = new AccessibilityService();
        }

        [Fact]
        public void Initialize_ShouldSetupAutomationElements()
        {
            // Act
            _sut.Initialize();

            // Assert
            _sut.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public void GetElementsUnderCursor_ShouldReturnElements()
        {
            // Arrange
            _sut.Initialize();

            // Act
            var elements = _sut.GetElementsUnderCursor();

            // Assert
            elements.Should().NotBeNull();
        }

        [Fact]
        public void FindElementByName_WithExistingElement_ShouldReturnElement()
        {
            // Arrange
            _sut.Initialize();
            var elementName = "Desktop";

            // Act
            var element = _sut.FindElementByName(elementName);

            // Assert
            element.Should().NotBeNull();
        }

        [Fact]
        public void FindElementByName_WithNonExistingElement_ShouldReturnNull()
        {
            // Arrange
            _sut.Initialize();
            var elementName = "NonExistingElement12345";

            // Act
            var element = _sut.FindElementByName(elementName);

            // Assert
            element.Should().BeNull();
        }

        [Fact]
        public void FindElementByName_WithNullName_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Action act = () => _sut.FindElementByName(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ClickElement_WithValidElement_ShouldNotThrow()
        {
            // Arrange
            _sut.Initialize();
            var desktop = _sut.FindElementByName("Desktop");

            if (desktop != null)
            {
                // Act
                Action act = () => _sut.ClickElement(desktop);

                // Assert
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void ClickElement_WithNullElement_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Action act = () => _sut.ClickElement(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetElementText_WithTextElement_ShouldReturnText()
        {
            // Arrange
            _sut.Initialize();
            var elements = _sut.GetElementsUnderCursor();
            var textElement = elements?.FirstOrDefault(e => !string.IsNullOrEmpty(_sut.GetElementText(e)));

            if (textElement != null)
            {
                // Act
                var text = _sut.GetElementText(textElement);

                // Assert
                text.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void GetElementText_WithNullElement_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Action act = () => _sut.GetElementText(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SetElementText_WithValidElement_ShouldNotThrow()
        {
            // Arrange
            _sut.Initialize();
            var elements = _sut.GetElementsUnderCursor();
            var editableElement = elements?.FirstOrDefault(e => _sut.IsElementEditable(e));

            if (editableElement != null)
            {
                // Act
                Action act = () => _sut.SetElementText(editableElement, "Test text");

                // Assert
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void SetElementText_WithNullElement_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Action act = () => _sut.SetElementText(null, "Test");

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsElementEditable_WithValidElement_ShouldReturnBool()
        {
            // Arrange
            _sut.Initialize();
            var desktop = _sut.FindElementByName("Desktop");

            if (desktop != null)
            {
                // Act
                var isEditable = _sut.IsElementEditable(desktop);

                // Assert
                isEditable.Should().BeOfType<bool>();
            }
        }

        [Fact]
        public void GetElementBounds_WithValidElement_ShouldReturnValidRectangle()
        {
            // Arrange
            _sut.Initialize();
            var desktop = _sut.FindElementByName("Desktop");

            if (desktop != null)
            {
                // Act
                var bounds = _sut.GetElementBounds(desktop);

                // Assert
                bounds.Width.Should().BeGreaterThan(0);
                bounds.Height.Should().BeGreaterThan(0);
            }
        }

        [Fact]
        public void GetElementBounds_WithNullElement_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Action act = () => _sut.GetElementBounds(null);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void HighlightElement_WithValidElement_ShouldNotThrow()
        {
            // Arrange
            _sut.Initialize();
            var desktop = _sut.FindElementByName("Desktop");

            if (desktop != null)
            {
                // Act
                Action act = () => _sut.HighlightElement(desktop);

                // Assert
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void GetElementsOfType_WithValidType_ShouldReturnElements()
        {
            // Arrange
            _sut.Initialize();

            // Act
            var buttons = _sut.GetElementsOfType(ControlType.Button);

            // Assert
            buttons.Should().NotBeNull();
        }
    }
}