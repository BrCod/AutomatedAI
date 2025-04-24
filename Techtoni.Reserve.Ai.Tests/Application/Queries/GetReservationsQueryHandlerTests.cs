using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Queries;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Queries;

public class GetReservationsQueryHandlerTests
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly GetReservationsQueryHandler _handler;

    public GetReservationsQueryHandlerTests()
    {
        // Setup mocks
        _reservationRepository = Substitute.For<IReservationRepository>();
        _resourceRepository = Substitute.For<IResourceRepository>();
        _handler = new GetReservationsQueryHandler(_reservationRepository, _resourceRepository);
    }

    [Fact]
    public async Task Handle_WithExistingResource_ShouldReturnReservations()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(7);
        
        var resource = new Resource("Meeting Room A", "A spacious meeting room");
        
        var reservations = new List<Reservation>
        {
            new Reservation(
                resourceId,
                "John Doe",
                "john.doe@example.com",
                startDate.AddHours(1),
                startDate.AddHours(3)),
            new Reservation(
                resourceId,
                "Jane Smith",
                "jane.smith@example.com",
                startDate.AddHours(4),
                startDate.AddHours(6))
        };

        var query = new GetReservationsQuery 
        { 
            ResourceId = resourceId,
            StartDate = startDate,
            EndDate = endDate
        };

        _resourceRepository.GetByIdAsync(resourceId).Returns(resource);
        _reservationRepository.GetByResourceIdAndDateRangeAsync(resourceId, startDate, endDate).Returns(reservations);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(reservations[0]);
        result.Should().Contain(reservations[1]);
        
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
        await _reservationRepository.Received(1).GetByResourceIdAndDateRangeAsync(resourceId, startDate, endDate);
    }

    [Fact]
    public async Task Handle_WithNonExistingResource_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(7);

        var query = new GetReservationsQuery 
        { 
            ResourceId = resourceId,
            StartDate = startDate,
            EndDate = endDate
        };

        _resourceRepository.GetByIdAsync(resourceId).Returns((Resource)null);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Resource with ID {resourceId} not found");
        
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
        await _reservationRepository.DidNotReceive().GetByResourceIdAndDateRangeAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task Handle_WithInvalidDateRange_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(7);  // End date before start date
        var endDate = DateTime.UtcNow;

        var query = new GetReservationsQuery 
        { 
            ResourceId = resourceId,
            StartDate = startDate,
            EndDate = endDate
        };

        var resource = new Resource("Meeting Room A", "A spacious meeting room");
        _resourceRepository.GetByIdAsync(resourceId).Returns(resource);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("End date must be after start date*");
        
        await _resourceRepository.Received(1).GetByIdAsync(resourceId);
        await _reservationRepository.DidNotReceive().GetByResourceIdAndDateRangeAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }
}