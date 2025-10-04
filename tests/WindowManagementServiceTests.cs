using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;
using System.Diagnostics;

namespace AionDesktopAssistant.Tests
{
    public class WindowManagementServiceTests
    {
        private readonly WindowManagementService _sut;

        public WindowManagementServiceTests()
        {
            _sut = new WindowManagementService();
        }

        [Fact]
        public void GetAllWindows_ShouldReturnNonEmptyCollection()
        {
            // Act
            var windows = _sut.GetAllWindows();

            // Assert
            windows.Should().NotBeNull();
            windows.Should().NotBeEmpty();
        }

        [Fact]
        public void GetActiveWindow_ShouldReturnValidHandle()
        {
            // Act
            var activeWindow = _sut.GetActiveWindow();

            // Assert
            activeWindow.Should().NotBe(IntPtr.Zero);
        }

        [Fact]
        public void GetWindowTitle_WithValidHandle_ShouldReturnTitle()
        {
            // Arrange
            var activeWindow = _sut.GetActiveWindow();

            // Act
            var title = _sut.GetWindowTitle(activeWindow);

            // Assert
            title.Should().NotBeNull();
        }

        [Fact]
        public void GetWindowTitle_WithInvalidHandle_ShouldReturnEmpty()
        {
            // Arrange
            var invalidHandle = IntPtr.Zero;

            // Act
            var title = _sut.GetWindowTitle(invalidHandle);

            // Assert
            title.Should().BeEmpty();
        }

        [Fact]
        public void SetWindowActive_WithValidHandle_ShouldNotThrow()
        {
            // Arrange
            var activeWindow = _sut.GetActiveWindow();

            // Act
            Action act = () => _sut.SetWindowActive(activeWindow);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SetWindowActive_WithInvalidHandle_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidHandle = IntPtr.Zero;

            // Act
            Action act = () => _sut.SetWindowActive(invalidHandle);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void MinimizeWindow_WithValidHandle_ShouldNotThrow()
        {
            // Arrange
            var windows = _sut.GetAllWindows();
            if (windows.Any())
            {
                var windowHandle = windows.First();

                // Act
                Action act = () => _sut.MinimizeWindow(windowHandle);

                // Assert
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void MaximizeWindow_WithValidHandle_ShouldNotThrow()
        {
            // Arrange
            var windows = _sut.GetAllWindows();
            if (windows.Any())
            {
                var windowHandle = windows.First();

                // Act
                Action act = () => _sut.MaximizeWindow(windowHandle);

                // Assert
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void RestoreWindow_WithValidHandle_ShouldNotThrow()
        {
            // Arrange
            var windows = _sut.GetAllWindows();
            if (windows.Any())
            {
                var windowHandle = windows.First();

                // Act
                Action act = () => _sut.RestoreWindow(windowHandle);

                // Assert
                act.Should().NotThrow();
            }
        }

        [Fact]
        public void CloseWindow_WithInvalidHandle_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidHandle = IntPtr.Zero;

            // Act
            Action act = () => _sut.CloseWindow(invalidHandle);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FindWindowByTitle_WithExistingTitle_ShouldReturnValidHandle()
        {
            // Arrange
            var activeWindow = _sut.GetActiveWindow();
            var title = _sut.GetWindowTitle(activeWindow);

            // Act
            var foundWindow = _sut.FindWindowByTitle(title);

            // Assert
            foundWindow.Should().NotBe(IntPtr.Zero);
            foundWindow.Should().Be(activeWindow);
        }

        [Fact]
        public void FindWindowByTitle_WithNonExistingTitle_ShouldReturnZero()
        {
            // Arrange
            var nonExistingTitle = "NonExistingWindowTitle12345";

            // Act
            var foundWindow = _sut.FindWindowByTitle(nonExistingTitle);

            // Assert
            foundWindow.Should().Be(IntPtr.Zero);
        }

        [Fact]
        public void GetWindowBounds_WithValidHandle_ShouldReturnValidRectangle()
        {
            // Arrange
            var activeWindow = _sut.GetActiveWindow();

            // Act
            var bounds = _sut.GetWindowBounds(activeWindow);

            // Assert
            bounds.Width.Should().BeGreaterThan(0);
            bounds.Height.Should().BeGreaterThan(0);
        }

        [Fact]
        public void IsWindowVisible_WithValidHandle_ShouldReturnBool()
        {
            // Arrange
            var activeWindow = _sut.GetActiveWindow();

            // Act
            var isVisible = _sut.IsWindowVisible(activeWindow);

            // Assert
            isVisible.Should().BeOfType<bool>();
        }
    }
}