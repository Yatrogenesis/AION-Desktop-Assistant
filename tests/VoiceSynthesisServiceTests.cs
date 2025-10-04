using Xunit;
using Moq;
using FluentAssertions;
using AionDesktopAssistant.Services;
using System.Speech.Synthesis;

namespace AionDesktopAssistant.Tests
{
    public class VoiceSynthesisServiceTests
    {
        private readonly VoiceSynthesisService _sut;

        public VoiceSynthesisServiceTests()
        {
            _sut = new VoiceSynthesisService();
        }

        [Fact]
        public void Initialize_ShouldSetupSynthesizer()
        {
            // Act
            _sut.Initialize();

            // Assert
            _sut.IsInitialized.Should().BeTrue();
        }

        [Fact]
        public async Task SpeakAsync_WithValidText_ShouldCompleteSuccessfully()
        {
            // Arrange
            _sut.Initialize();
            var text = "Hello world";

            // Act
            Func<Task> act = async () => await _sut.SpeakAsync(text);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SpeakAsync_WithNullText_ShouldThrowArgumentNullException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Func<Task> act = async () => await _sut.SpeakAsync(null);

            // Assert
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task SpeakAsync_WithEmptyText_ShouldThrowArgumentException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Func<Task> act = async () => await _sut.SpeakAsync(string.Empty);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public void SetVoice_WithValidVoiceName_ShouldChangeVoice()
        {
            // Arrange
            _sut.Initialize();
            var availableVoices = _sut.GetAvailableVoices();
            if (availableVoices.Any())
            {
                var voiceName = availableVoices.First();

                // Act
                _sut.SetVoice(voiceName);

                // Assert
                _sut.CurrentVoice.Should().Be(voiceName);
            }
        }

        [Fact]
        public void SetVoice_WithInvalidVoiceName_ShouldThrowArgumentException()
        {
            // Arrange
            _sut.Initialize();
            var invalidVoiceName = "NonExistentVoice";

            // Act
            Action act = () => _sut.SetVoice(invalidVoiceName);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void SetVolume_WithValidValue_ShouldUpdateVolume()
        {
            // Arrange
            _sut.Initialize();
            var volume = 75;

            // Act
            _sut.SetVolume(volume);

            // Assert
            _sut.Volume.Should().Be(volume);
        }

        [Fact]
        public void SetVolume_WithInvalidValue_ShouldThrowArgumentOutOfRangeException()
        {
            // Arrange
            _sut.Initialize();

            // Act
            Action act = () => _sut.SetVolume(150);

            // Assert
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void SetRate_WithValidValue_ShouldUpdateRate()
        {
            // Arrange
            _sut.Initialize();
            var rate = 2;

            // Act
            _sut.SetRate(rate);

            // Assert
            _sut.Rate.Should().Be(rate);
        }

        [Fact]
        public void GetAvailableVoices_ShouldReturnNonEmptyCollection()
        {
            // Arrange
            _sut.Initialize();

            // Act
            var voices = _sut.GetAvailableVoices();

            // Assert
            voices.Should().NotBeEmpty();
        }
    }
}