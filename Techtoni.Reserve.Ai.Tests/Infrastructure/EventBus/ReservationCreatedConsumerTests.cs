using System;
using System.Threading.Tasks;
using MassTransit;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Interfaces;
using Techtoni.Reserve.Ai.Domain.Events;
using Techtoni.Reserve.Ai.Infrastructure.EventBus;

namespace Techtoni.Reserve.Ai.Tests.Infrastructure.EventBus;

public class ReservationCreatedConsumerTests
{
    private readonly INotificationService _notificationService;
    private readonly ReservationCreatedConsumer _consumer;

    public ReservationCreatedConsumerTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        _consumer = new ReservationCreatedConsumer(_notificationService);
    }

    [Fact]
    public async Task Consume_ShouldCallSendReservationConfirmationAsync()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = "john.doe@example.com";
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);
        var createdAt = DateTime.UtcNow;

        var @event = new ReservationCreatedEvent(
            reservationId,
            resourceId,
            customerName,
            customerEmail,
            startTime,
            endTime,
            createdAt);

        var context = Substitute.For<ConsumeContext<ReservationCreatedEvent>>();
        context.Message.Returns(@event);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _notificationService.Received(1).SendReservationConfirmationAsync(
            reservationId,
            customerName,
            customerEmail,
            startTime,
            endTime);
    }
}