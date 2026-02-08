using FluentAssertions;
using Moq;
using SheroShayari.API.Controllers;
using SheroShayari.API.Models;
using SheroShayari.API.Repositories;
using SheroShayari.API.Services;
using Xunit;
using Microsoft.Extensions.Logging;

namespace SheroShayari.Tests.Controllers;

public class ShayariControllerTests
{
    private readonly Mock<IShayariRepository> _repositoryMock;
    private readonly Mock<IAiGenerationService> _aiServiceMock;
    private readonly Mock<ILogger<ShayariController>> _loggerMock;
    private readonly ShayariController _controller;

    public ShayariControllerTests()
    {
        _repositoryMock = new Mock<IShayariRepository>();
        _aiServiceMock = new Mock<IAiGenerationService>();
        _loggerMock = new Mock<ILogger<ShayariController>>();
        _controller = new ShayariController(_repositoryMock.Object, _aiServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void ShayariController_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var controller = _controller;

        // Assert
        controller.Should().NotBeNull();
        controller.Should().BeOfType<ShayariController>();
    }

    [Fact]
    public void ShayariController_WithValidDependencies_ShouldConstruct()
    {
        // Arrange & Act & Assert
        new ShayariController(_repositoryMock.Object, _aiServiceMock.Object, _loggerMock.Object).Should().NotBeNull();
    }
}

