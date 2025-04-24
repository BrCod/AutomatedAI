using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Queries;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Queries;

public class GetResourcesQueryHandlerTests
{
    private readonly IResourceRepository _resourceRepository;
    private readonly GetResourcesQueryHandler _handler;

    public GetResourcesQueryHandlerTests()
    {
        // Setup mocks
        _resourceRepository = Substitute.For<IResourceRepository>();
        _handler = new GetResourcesQueryHandler(_resourceRepository);
    }

    [Fact]
    public async Task Handle_WithOnlyAvailableFalse_ShouldReturnAllResources()
    {
        // Arrange
        var allResources = new List<Resource>
        {
            new Resource("Meeting Room A", "A spacious meeting room"),
            new Resource("Conference Room B", "Large conference room")
        };
        
        // Make the second resource unavailable
        allResources[1].SetAvailability(false);

        var query = new GetResourcesQuery { OnlyAvailable = false };
        _resourceRepository.GetAllAsync().Returns(allResources);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(allResources[0]);
        result.Should().Contain(allResources[1]);
        await _resourceRepository.Received(1).GetAllAsync();
        await _resourceRepository.DidNotReceive().GetAvailableAsync();
    }

    [Fact]
    public async Task Handle_WithOnlyAvailableTrue_ShouldReturnOnlyAvailableResources()
    {
        // Arrange
        var availableResources = new List<Resource>
        {
            new Resource("Meeting Room A", "A spacious meeting room")
        };

        var query = new GetResourcesQuery { OnlyAvailable = true };
        _resourceRepository.GetAvailableAsync().Returns(availableResources);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().Contain(availableResources[0]);
        await _resourceRepository.Received(1).GetAvailableAsync();
        await _resourceRepository.DidNotReceive().GetAllAsync();
    }
}