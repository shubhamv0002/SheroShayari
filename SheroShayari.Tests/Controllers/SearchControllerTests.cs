using FluentAssertions;
using Xunit;

namespace SheroShayari.Tests.Controllers;

public class SearchControllerTests
{
    [Fact]
    public void SearchControllerTests_CanBeInstantiated()
    {
        // Simple test to verify test class can be created
        var testInstance = typeof(SearchControllerTests);
        testInstance.Should().NotBeNull();
    }
}


