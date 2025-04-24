using System;
using System.Threading.Tasks;
using MassTransit;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Interfaces;
using Techtoni.Reserve.Ai.Domain.Events;
using Techtoni.Reserve.Ai.Infrastructure.EventBus;

namespace Techtoni.Reserve.Ai.Tests.Infrastructure.EventBus;

public class ReservationCancelledConsumerTests
{
    private readonly INotificationService _notificationService;
    private readonly ReservationCancelledConsumer _consumer;

    public ReservationCancelledConsumerTests()
    {
        _notificationService = Substitute.For<INotificationService>();
        _consumer = new ReservationCancelledConsumer(_notificationService);
    }

    [Fact]
    public async Task Consume_ShouldCallSendReservationCancellationAsync()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = "john.doe@example.com";
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);
        var cancelledAt = DateTime.UtcNow;

        var @event = new ReservationCancelledEvent(
            reservationId,
            resourceId,
            customerName,
            customerEmail,
            startTime,
            endTime,
            cancelledAt);

        var context = Substitute.For<ConsumeContext<ReservationCancelledEvent>>();
        context.Message.Returns(@event);

        // Act
        await _consumer.Consume(context);

        // Assert
        await _notificationService.Received(1).SendReservationCancellationAsync(
            reservationId,
            customerName,
            customerEmail,
            startTime,
            endTime);
    }
}