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

public class ConfirmReservationCommandHandlerTests
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IEventBus _eventBus;
    private readonly ConfirmReservationCommandHandler _handler;

    public ConfirmReservationCommandHandlerTests()
    {
        // Setup mocks
        _reservationRepository = Substitute.For<IReservationRepository>();
        _eventBus = Substitute.For<IEventBus>();
        _handler = new ConfirmReservationCommandHandler(_reservationRepository, _eventBus);
    }

    [Fact]
    public async Task Handle_WithExistingReservation_ShouldConfirmAndReturnTrue()
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

        var command = new ConfirmReservationCommand
        {
            ReservationId = reservationId
        };

        _reservationRepository.GetByIdAsync(reservationId).Returns(reservation);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        
        await _reservationRepository.Received(1).GetByIdAsync(reservationId);
        await _reservationRepository.Received(1).UpdateAsync(reservation);
        await _reservationRepository.Received(1).SaveChangesAsync();
        
        await _eventBus.Received(1).PublishAsync(Arg.Is<ReservationConfirmedEvent>(e => 
            e.ReservationId == reservationId &&
            e.ResourceId == resourceId));
    }

    [Fact]
    public async Task Handle_WithNonExistingReservation_ShouldReturnFalse()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var command = new ConfirmReservationCommand
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
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ReservationConfirmedEvent>());
    }

    [Fact]
    public async Task Handle_WithAlreadyConfirmedReservation_ShouldReturnTrue()
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
        
        // Already confirmed
        reservation.Confirm();

        var command = new ConfirmReservationCommand
        {
            ReservationId = reservationId
        };

        _reservationRepository.GetByIdAsync(reservationId).Returns(reservation);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        
        await _reservationRepository.Received(1).GetByIdAsync(reservationId);
        await _reservationRepository.Received(1).UpdateAsync(reservation);
        await _reservationRepository.Received(1).SaveChangesAsync();
        
        // No need to publish event if already confirmed
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ReservationConfirmedEvent>());
    }

    [Fact]
    public async Task Handle_WithCancelledReservation_ShouldNotChangeStatusAndReturnFalse()
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
        
        // Cancelled reservation
        reservation.Cancel();

        var command = new ConfirmReservationCommand
        {
            ReservationId = reservationId
        };

        _reservationRepository.GetByIdAsync(reservationId).Returns(reservation);
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        
        await _reservationRepository.Received(1).GetByIdAsync(reservationId);
        await _reservationRepository.DidNotReceive().UpdateAsync(Arg.Any<Reservation>());
        await _reservationRepository.DidNotReceive().SaveChangesAsync();
        await _eventBus.DidNotReceive().PublishAsync(Arg.Any<ReservationConfirmedEvent>());
    }
}