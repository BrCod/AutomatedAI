using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Queries;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Queries;

public class CheckResourceAvailabilityQueryHandlerTests
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly CheckResourceAvailabilityQueryHandler _handler;

    public CheckResourceAvailabilityQueryHandlerTests()
    {
        // Setup mocks
        _resourceRepository = Substitute.For<IResourceRepository>();
        _reservationRepository = Substitute.For<IReservationRepository>();
        _handler = new CheckResourceAvailabilityQueryHandler(_resourceRepository, _reservationRepository);
    }

    [Fact]
    public async Task Handle_WithAvailableResource_ShouldReturnTrue()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        var resource = new Resource("Test Resource", "Test Description");
        
        var query = new CheckResourceAvailabilityQuery 
        { 
            ResourceId = resourceId,
            StartTime = startTime,
            EndTime = endTime
        };

        _resourceRepository.GetByIdAsync(resourceId).Returns(resource);
        _reservationRepository.GetConflictingReservationsAsync(resourceId, startTime, endTime)
            .Returns(new List<Reservation>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
        await _reservationRepository.Received(1).GetConflictingReservationsAsync(resourceId, startTime, endTime);
    }

    [Fact]
    public async Task Handle_WithUnavailableResource_ShouldReturnFalse()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        var resource = new Resource("Test Resource", "Test Description") { };
        resource.MakeUnavailable();
        
        var query = new CheckResourceAvailabilityQuery 
        { 
            ResourceId = resourceId,
            StartTime = startTime,
            EndTime = endTime
        };

        _resourceRepository.GetByIdAsync(resourceId).Returns(resource);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
        await _reservationRepository.DidNotReceive().GetConflictingReservationsAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Handle_WithNonExistentResource_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        var query = new CheckResourceAvailabilityQuery 
        { 
            ResourceId = resourceId,
            StartTime = startTime,
            EndTime = endTime
        };

        _resourceRepository.GetByIdAsync(resourceId).Returns((Resource)null);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Resource with ID {resourceId} not found");
    }
}