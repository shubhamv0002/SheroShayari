using FluentAssertions;
using Moq;
using SheroShayari.API.Services;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SheroShayari.Tests.Services;

public class AiGenerationServiceTests
{
    private readonly Mock<HttpClient> _httpClientMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<AiGenerationService>> _loggerMock;
    private readonly AiGenerationService _service;

    public AiGenerationServiceTests()
    {
        _httpClientMock = new Mock<HttpClient>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<AiGenerationService>>();
        
        _service = new AiGenerationService(
            new HttpClient(),
            _configurationMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public void AiGenerationService_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var service = _service;

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void AiGenerationService_ShouldHaveInterface()
    {
        // Arrange & Act
        var service = (IAiGenerationService)_service;

        // Assert
        service.Should().NotBeNull();
    }

    [Theory]
    [InlineData("Love")]
    [InlineData("Loss")]
    [InlineData("Hope")]
    public void AiGenerationService_WithValidTheme_ShouldInitialize(string theme)
    {
        // Arrange & Act & Assert
        _service.Should().NotBeNull();
        theme.Should().NotBeNullOrEmpty();
    }
}


