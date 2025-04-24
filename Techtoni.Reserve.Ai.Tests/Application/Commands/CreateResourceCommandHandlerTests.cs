using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Commands;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Commands;

public class CreateResourceCommandHandlerTests
{
    private readonly IResourceRepository _resourceRepository;
    private readonly CreateResourceCommandHandler _handler;

    public CreateResourceCommandHandlerTests()
    {
        // Setup mocks
        _resourceRepository = Substitute.For<IResourceRepository>();
        _handler = new CreateResourceCommandHandler(_resourceRepository);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateResourceAndReturnId()
    {
        // Arrange
        var name = "Meeting Room A";
        var description = "A spacious meeting room with video conferencing equipment";

        var command = new CreateResourceCommand
        {
            Name = name,
            Description = description
        };
        
        _resourceRepository.When(r => r.AddAsync(Arg.Any<Resource>()))
            .Do(callInfo => {
                var resource = callInfo.Arg<Resource>();
                resource.Should().NotBeNull();
                resource.Name.Should().Be(name);
                resource.Description.Should().Be(description);
                resource.IsAvailable.Should().BeTrue();
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        
        await _resourceRepository.Received(1).AddAsync(Arg.Any<Resource>());
        await _resourceRepository.Received(1).SaveChangesAsync();
    }

    [Theory]
    [InlineData("", "Description")]
    [InlineData(" ", "Description")]
    [InlineData(null, "Description")]
    public async Task Handle_WithInvalidName_ShouldThrowArgumentException(string name, string description)
    {
        // Arrange
        var command = new CreateResourceCommand
        {
            Name = name,
            Description = description
        };

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Resource name cannot be empty*");
    }
}