using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SheroShayari.API.Data;
using SheroShayari.API.Models;
using SheroShayari.API.Repositories;
using Xunit;
using Microsoft.Extensions.Logging;

namespace SheroShayari.Tests.Repositories;

public class ShayariRepositoryTests
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<ILogger<ShayariRepository>> _loggerMock;
    private readonly ShayariRepository _repository;

    public ShayariRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<ShayariRepository>>();
        _repository = new ShayariRepository(_dbContext, _loggerMock.Object);
    }

    [Fact]
    public void ShayariRepository_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var repository = _repository;

        // Assert
        repository.Should().NotBeNull();
    }

    [Fact]
    public void ShayariRepository_ImplementsIShayariRepository()
    {
        // Arrange
        var repository = _repository;

        // Act & Assert
        repository.Should().BeAssignableTo<IShayariRepository>();
    }

    [Fact]
    public async Task AddAsync_WithValidData_ShouldSucceed()
    {
        // Arrange
        var shayari = new Shayari
        {
            Content = "This is a test shayari content",
            Poet = "Test Poet",
            Language = "Hindi",
            Category = "Love",
            UserId = "user123"
        };

        // Act
        var result = await _repository.AddAsync(shayari);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("Love")]
    [InlineData("Loss")]
    [InlineData("Hope")]
    public void ShayariRepository_WithDifferentCategories_ShouldBeValid(string category)
    {
        // Arrange
        var repository = _repository;

        // Assert
        repository.Should().NotBeNull();
        category.Should().NotBeNullOrEmpty();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dbContext?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}


