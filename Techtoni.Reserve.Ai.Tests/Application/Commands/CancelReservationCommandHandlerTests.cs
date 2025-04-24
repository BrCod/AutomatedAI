using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Commands;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Events;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Commands;

public class CancelReservationCommandHandlerTests
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IEventBus _eventBus;
    private readonly CancelReservationCommandHandler _handler;

    public CancelReservationCommandHandlerTests()
    {
        // Setup mocks
        _reservationRepository = Substitute.For<IReservationRepository>();
        _eventBus = Substitute.For<IEventBus>();
        _handler = new CancelReservationCommandHandler(_reservationRepository, _eventBus);
    }

    [Fact]
    public async Task Handle_WithExistingReservation_ShouldCancelAndReturnTrue()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var reservation = new Reservation(
            resourceId,
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));

        var command = new CancelReservationCommand
        {
            ReservationId = reservationId
        };

        _reservationRepository.GetByIdAsync(reservationId).Returns(reservation);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        
        await _reservationRepository.Received(1).GetByIdAsync(reservationId);
        await _reservationRepository.Received(1).UpdateAsync(reservation);
        await _reservationRepository.Received(1).SaveChangesAsync();
        
        await _eventBus.Received(1).PublishAsync(Arg.Is<ReservationCancelledEvent>(e => 
            e.ReservationId == reservationId &&
            e.ResourceId == resourceId));
    }

    [Fact]
    public async Task Handle_WithNonExistingReservation_ShouldReturnFalse()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var command = new CancelReservationCommand
        {
            ReservationId = reservationId
        };

        _reservationRepository.GetByIdAsync(reservationId).Returns((Reservation)null);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        
        await _reservationRepository.Received(1).GetByIdAsync(reservationId);
        await _reservationRepository.DidNotReceive().UpdateAsync(Arg.Any<Reservation>());
        await _reservationRepository.DidNotReceive().SaveChangesAsync();
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ReservationCancelledEvent>());
    }

    [Fact]
    public async Task Handle_WithAlreadyCancelledReservation_ShouldReturnTrue()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var reservation = new Reservation(
            resourceId,
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));
        
        // Already cancelled
        reservation.Cancel();

        var command = new CancelReservationCommand
        {
            ReservationId = reservationId
        };

        _reservationRepository.GetByIdAsync(reservationId).Returns(reservation);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        
        await _reservationRepository.Received(1).GetByIdAsync(reservationId);
        await _reservationRepository.Received(1).UpdateAsync(reservation);
        await _reservationRepository.Received(1).SaveChangesAsync();
        
        // No need to publish event if already cancelled
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ReservationCancelledEvent>());
    }
}