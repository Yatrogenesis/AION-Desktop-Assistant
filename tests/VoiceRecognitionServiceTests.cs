using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;
using System.Speech.Recognition;

namespace AionDesktopAssistant.Tests
{
    public class VoiceRecognitionServiceTests
    {
        private readonly VoiceRecognitionService _sut;

        public VoiceRecognitionServiceTests()
        {
            _sut = new VoiceRecognitionService();
        }

        [Fact]
        public void Initialize_ShouldSetupRecognitionEngine()
        {
            // Act
            _sut.Initialize();

            // Assert
            _sut.IsListening.Should().BeFalse(); // Initially not listening
        }

        [Fact]
        public void StartListening_WhenInitialized_ShouldStartRecognition()
        {
            // Arrange
            _sut.Initialize();

            // Act
            _sut.StartListening();

            // Assert
            _sut.IsListening.Should().BeTrue();
        }

        [Fact]
        public void StopListening_WhenListening_ShouldStopRecognition()
        {
            // Arrange
            _sut.Initialize();
            _sut.StartListening();

            // Act
            _sut.StopListening();

            // Assert
            _sut.IsListening.Should().BeFalse();
        }

        [Fact]
        public void StartListening_WhenNotInitialized_ShouldThrowInvalidOperationException()
        {
            // Act
            Action act = () => _sut.StartListening();

            // Assert
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void AddCommand_WithValidParameters_ShouldAddToGrammar()
        {
            // Arrange
            _sut.Initialize();
            var command = "test command";
            var action = () => { };

            // Act
            _sut.AddCommand(command, action);

            // Assert
            // Command should be added successfully (no exception thrown)
            _sut.Commands.Should().ContainKey(command);
        }

        [Fact]
        public void AddCommand_WithNullCommand_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sut.Initialize();
            var action = () => { };

            // Act
            Action act = () => _sut.AddCommand(null, action);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SetConfidenceThreshold_WithValidValue_ShouldUpdateThreshold()
        {
            // Arrange
            _sut.Initialize();
            var threshold = 0.8f;

            // Act
            _sut.SetConfidenceThreshold(threshold);

            // Assert
            _sut.ConfidenceThreshold.Should().Be(threshold);
        }

        [Fact]
        public void SetConfidenceThreshold_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Action act = () => _sut.SetConfidenceThreshold(1.5f);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}