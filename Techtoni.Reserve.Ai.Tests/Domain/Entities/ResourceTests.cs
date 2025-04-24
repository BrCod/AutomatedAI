using System;
using FluentAssertions;
using Techtoni.Reserve.Ai.Domain.Entities;

namespace Techtoni.Reserve.Ai.Tests.Domain.Entities;

public class ResourceTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateResource()
    {
        // Arrange
        var name = "Meeting Room A";
        var description = "A spacious meeting room with video conferencing equipment";

        // Act
        var resource = new Resource(name, description);

        // Assert
        resource.Should().NotBeNull();
        resource.Id.Should().NotBeEmpty();
        resource.Name.Should().Be(name);
        resource.Description.Should().Be(description);
        resource.IsAvailable.Should().BeTrue();
        resource.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        resource.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Constructor_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var description = "A spacious meeting room with video conferencing equipment";

        // Act & Assert
        Action act = () => new Resource(name, description);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Resource name cannot be empty*")
            .WithParameterName("name");
    }

    [Fact]
    public void SetAvailability_ShouldUpdateAvailabilityAndUpdatedAt()
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "A spacious meeting room");
        var initialUpdatedAt = resource.UpdatedAt;

        // Act
        resource.SetAvailability(false);

        // Assert
        resource.IsAvailable.Should().BeFalse();
        resource.UpdatedAt.Should().NotBe(initialUpdatedAt);
        resource.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateResourceAndUpdatedAt()
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "Original description");
        var newName = "Meeting Room B";
        var newDescription = "Updated description";
        var initialUpdatedAt = resource.UpdatedAt;

        // Act
        resource.Update(newName, newDescription);

        // Assert
        resource.Name.Should().Be(newName);
        resource.Description.Should().Be(newDescription);
        resource.UpdatedAt.Should().NotBe(initialUpdatedAt);
        resource.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Update_WithInvalidName_ShouldThrowArgumentException(string name)
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "Original description");
        var newDescription = "Updated description";

        // Act & Assert
        Action act = () => resource.Update(name, newDescription);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Resource name cannot be empty*")
            .WithParameterName("name");
    }
}