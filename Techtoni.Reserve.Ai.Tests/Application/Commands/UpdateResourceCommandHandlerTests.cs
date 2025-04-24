using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Commands;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Commands;

public class UpdateResourceCommandHandlerTests
{
    private readonly IResourceRepository _resourceRepository;
    private readonly UpdateResourceCommandHandler _handler;

    public UpdateResourceCommandHandlerTests()
    {
        // Setup mocks
        _resourceRepository = Substitute.For<IResourceRepository>();
        _handler = new UpdateResourceCommandHandler(_resourceRepository);
    }

    [Fact]
    public async Task Handle_WithExistingResource_ShouldUpdateAndReturnTrue()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var name = "Updated Meeting Room";
        var description = "Updated description";
        var isAvailable = false;

        var command = new UpdateResourceCommand
        {
            Id = resourceId,
            Name = name,
            Description = description,
            IsAvailable = isAvailable
        };

        var existingResource = new Resource("Original Meeting Room", "Original description");
        _resourceRepository.GetByIdAsync(resourceId).Returns(existingResource);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        
        existingResource.Name.Should().Be(name);
        existingResource.Description.Should().Be(description);
        existingResource.IsAvailable.Should().Be(isAvailable);
        
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
        await _resourceRepository.Received(1).UpdateAsync(existingResource);
        await _resourceRepository.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_WithNonExistingResource_ShouldReturnFalse()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var command = new UpdateResourceCommand
        {
            Id = resourceId,
            Name = "Updated Meeting Room",
            Description = "Updated description",
            IsAvailable = false
        };

        _resourceRepository.GetByIdAsync(resourceId).Returns((Resource)null);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
        await _resourceRepository.DidNotReceive().UpdateAsync(Arg.Any<Resource>());
        await _resourceRepository.DidNotReceive().SaveChangesAsync();
    }

    [Theory]
    [InlineData("", "Updated description")]
    [InlineData(" ", "Updated description")]
    [InlineData(null, "Updated description")]
    public async Task Handle_WithInvalidName_ShouldThrowArgumentException(string name, string description)
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var command = new UpdateResourceCommand
        {
            Id = resourceId,
            Name = name,
            Description = description,
            IsAvailable = true
        };

        var existingResource = new Resource("Original Meeting Room", "Original description");
        _resourceRepository.GetByIdAsync(resourceId).Returns(existingResource);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Resource name cannot be empty*");
    }
}