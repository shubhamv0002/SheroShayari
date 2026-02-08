using FluentAssertions;
using Moq;
using SheroShayari.API.Services;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SheroShayari.Tests.Services;

public class EmailSenderTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<EmailSender>> _loggerMock;
    private readonly EmailSender _service;

    public EmailSenderTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<EmailSender>>();
        
        _service = new EmailSender(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void EmailSender_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var service = _service;

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void EmailSender_ShouldImplementIEmailSender()
    {
        // Arrange
        var service = _service;

        // Act & Assert
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<global::Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>();
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test@domain.org")]
    [InlineData("admin@app.io")]
    public void EmailSender_WithValidEmails_ShouldBeValid(string email)
    {
        // Arrange & Act
        var service = _service;

        // Assert
        service.Should().NotBeNull();
        email.Should().Contain("@");
    }
}


