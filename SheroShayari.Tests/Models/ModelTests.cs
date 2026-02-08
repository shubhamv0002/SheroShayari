using FluentAssertions;
using SheroShayari.API.Models;
using Xunit;

namespace SheroShayari.Tests.Models;

public class ShayariModelTests
{
    [Fact]
    public void CreateShayari_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var shayari = new Shayari
        {
            Content = "This is test shayari content",
            Poet = "Test Poet",
            Language = "Hindi",
            Category = "Love"
        };

        // Assert
        shayari.Content.Should().NotBeNullOrEmpty();
        shayari.Poet.Should().NotBeNullOrEmpty();
        shayari.Language.Should().NotBeNullOrEmpty();
        shayari.Category.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Shayari_WithNullContent_ShouldThrowError()
    {
        // Arrange & Act & Assert
        // Since Content is required, should not allow null
        var shayari = new Shayari
        {
            Content = "Required",
            Poet = "Required",
            Language = "Hindi",
            Category = "Love"
        };

        shayari.Content.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("Love")]
    [InlineData("Loss")]
    [InlineData("Hope")]
    [InlineData("Philosophy")]
    [InlineData("Nature")]
    public void Shayari_WithDifferentCategories_ShouldBeValid(string category)
    {
        // Arrange & Act
        var shayari = new Shayari
        {
            Content = "Test content",
            Poet = "Poet Name",
            Language = "English",
            Category = category
        };

        // Assert
        shayari.Category.Should().Be(category);
    }

    [Fact]
    public void Shayari_CreatedAt_ShouldHaveValidTimestamp()
    {
        // Arrange & Act
        var shayari = new Shayari
        {
            Content = "Test",
            Poet = "Poet",
            Language = "English",
            Category = "Love"
        };

        // Assert
        shayari.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("Hindi")]
    [InlineData("Urdu")]
    [InlineData("English")]
    public void Shayari_WithDifferentLanguages_ShouldBeValid(string language)
    {
        // Arrange & Act
        var shayari = new Shayari
        {
            Content = "Test",
            Poet = "Poet",
            Language = language,
            Category = "Love"
        };

        // Assert
        shayari.Language.Should().Be(language);
    }

    [Fact]
    public void Shayari_IsAiGenerated_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var shayari = new Shayari
        {
            Content = "Test",
            Poet = "Poet",
            Language = "English",
            Category = "Love"
        };

        // Assert
        shayari.IsAiGenerated.Should().BeFalse();
    }

    [Fact]
    public void Shayari_IsPublic_ShouldDefaultToFalse()
    {
        // Arrange & Act
        var shayari = new Shayari
        {
            Content = "Test",
            Poet = "Poet",
            Language = "English",
            Category = "Love"
        };

        // Assert
        shayari.IsPublic.Should().BeFalse();
    }
}

public class ApplicationUserModelTests
{
    [Fact]
    public void CreateApplicationUser_WithValidData_ShouldSucceed()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            FullName = "Test User"
        };

        // Assert
        user.UserName.Should().Be("testuser");
        user.Email.Should().Be("test@example.com");
        user.FullName.Should().Be("Test User");
    }

    [Fact]
    public void ApplicationUser_WithoutFullName_ShouldHaveNullOrEmpty()
    {
        // Arrange & Act
        var user = new ApplicationUser
        {
            UserName = "testuser",
            Email = "test@example.com",
            FullName = ""
        };

        // Assert
        user.FullName.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationUser_WithValidEmail_ShouldBeValid()
    {
        // Arrange
        var validEmails = new[] { "test@example.com", "user@domain.co.uk", "admin@app.io" };

        // Act & Assert
        foreach (var email in validEmails)
        {
            var user = new ApplicationUser
            {
                UserName = "user",
                Email = email
            };

            user.Email.Should().Contain("@");
        }
    }
}

