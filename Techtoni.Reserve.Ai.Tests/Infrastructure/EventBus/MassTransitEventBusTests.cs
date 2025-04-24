using System.Threading.Tasks;
using MassTransit;
using NSubstitute;
using Techtoni.Reserve.Ai.Domain.Events;
using Techtoni.Reserve.Ai.Infrastructure.EventBus;

namespace Techtoni.Reserve.Ai.Tests.Infrastructure.EventBus;

public class MassTransitEventBusTests
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly MassTransitEventBus _eventBus;

    public MassTransitEventBusTests()
    {
        _publishEndpoint = Substitute.For<IPublishEndpoint>();
        _eventBus = new MassTransitEventBus(_publishEndpoint);
    }

    [Fact]
    public async Task PublishAsync_ShouldCallPublishEndpoint()
    {
        // Arrange
        var reservationCreatedEvent = new ReservationCreatedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3),
            DateTime.UtcNow);

        // Act
        await _eventBus.PublishAsync(reservationCreatedEvent);

        // Assert
        await _publishEndpoint.Received(1).Publish(Arg.Is<ReservationCreatedEvent>(e => 
            e.ReservationId == reservationCreatedEvent.ReservationId &&
            e.ResourceId == reservationCreatedEvent.ResourceId &&
            e.CustomerName == reservationCreatedEvent.CustomerName &&
            e.CustomerEmail == reservationCreatedEvent.CustomerEmail &&
            e.StartTime == reservationCreatedEvent.StartTime &&
            e.EndTime == reservationCreatedEvent.EndTime &&
            e.CreatedAt == reservationCreatedEvent.CreatedAt
        ));
    }
}