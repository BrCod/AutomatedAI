using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Queries;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Queries;

public class GetReservationByIdQueryHandlerTests
{
    private readonly IReservationRepository _reservationRepository;
    private readonly GetReservationByIdQueryHandler _handler;

    public GetReservationByIdQueryHandlerTests()
    {
        // Setup mocks
        _reservationRepository = Substitute.For<IReservationRepository>();
        _handler = new GetReservationByIdQueryHandler(_reservationRepository);
    }

    [Fact]
    public async Task Handle_WithExistingId_ShouldReturnReservation()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = "john.doe@example.com";
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        var reservation = new Reservation(
            resourceId,
            customerName,
            customerEmail,
            startTime,
            endTime);

        var query = new GetReservationByIdQuery { Id = reservationId };
        _reservationRepository.GetByIdAsync(reservationId).Returns(reservation);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(reservation);
        await _reservationRepository.Received(1).GetByIdAsync(reservationId);
    }

    [Fact]
    public async Task Handle_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var query = new GetReservationByIdQuery { Id = reservationId };
        _reservationRepository.GetByIdAsync(reservationId).Returns((Reservation)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        await _reservationRepository.Received(1).GetByIdAsync(reservationId);
    }
}