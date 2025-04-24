using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Queries;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Queries;

public class GetResourceByIdQueryHandlerTests
{
    private readonly IResourceRepository _resourceRepository;
    private readonly GetResourceByIdQueryHandler _handler;

    public GetResourceByIdQueryHandlerTests()
    {
        // Setup mocks
        _resourceRepository = Substitute.For<IResourceRepository>();
        _handler = new GetResourceByIdQueryHandler(_resourceRepository);
    }

    [Fact]
    public async Task Handle_WithExistingId_ShouldReturnResource()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var resource = new Resource("Meeting Room A", "A spacious meeting room");

        var query = new GetResourceByIdQuery { Id = resourceId };
        _resourceRepository.GetByIdAsync(resourceId).Returns(resource);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(resource);
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
    }

    [Fact]
    public async Task Handle_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var query = new GetResourceByIdQuery { Id = resourceId };
        _resourceRepository.GetByIdAsync(resourceId).Returns((Resource)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
    }
}