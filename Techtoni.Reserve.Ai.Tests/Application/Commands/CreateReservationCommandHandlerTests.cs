using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using NSubstitute;
using Techtoni.Reserve.Ai.Application.Commands;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Events;
using Techtoni.Reserve.Ai.Domain.Interfaces;

namespace Techtoni.Reserve.Ai.Tests.Application.Commands;

public class CreateReservationCommandHandlerTests
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IEventBus _eventBus;
    private readonly CreateReservationCommandHandler _handler;

    public CreateReservationCommandHandlerTests()
    {
        // Setup mocks
        _reservationRepository = Substitute.For<IReservationRepository>();
        _resourceRepository = Substitute.For<IResourceRepository>();
        _eventBus = Substitute.For<IEventBus>();

        _handler = new CreateReservationCommandHandler(
            _reservationRepository,
            _resourceRepository,
            _eventBus);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCreateReservationAndReturnId()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = "john.doe@example.com";
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        var command = new CreateReservationCommand
        {
            ResourceId = resourceId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            StartTime = startTime,
            EndTime = endTime
        };

        var resource = new Resource("Test Resource", "Test Description");
        
        _resourceRepository.GetByIdAsync(resourceId).Returns(resource);
        _resourceRepository.IsResourceAvailableAsync(resourceId, startTime, endTime).Returns(true);
        
        _reservationRepository.When(r => r.AddAsync(Arg.Any<Reservation>()))
            .Do(callInfo => {
                var reservation = callInfo.Arg<Reservation>();
                reservation.Should().NotBeNull();
                reservation.ResourceId.Should().Be(resourceId);
                reservation.CustomerName.Should().Be(customerName);
                reservation.CustomerEmail.Should().Be(customerEmail);
                reservation.StartTime.Should().Be(startTime);
                reservation.EndTime.Should().Be(endTime);
            });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        
        await _reservationRepository.Received(1).AddAsync(Arg.Any<Reservation>());
        await _reservationRepository.Received(1).SaveChangesAsync();
        
        await _eventBus.Received(1).PublishAsync(Arg.Is<ReservationCreatedEvent>(e => 
            e.ReservationId == result && 
            e.ResourceId == resourceId &&
            e.CustomerName == customerName &&
            e.CustomerEmail == customerEmail &&
            e.StartTime == startTime &&
            e.EndTime == endTime));
    }

    [Fact]
    public async Task Handle_WithNonExistentResource_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var command = new CreateReservationCommand
        {
            ResourceId = resourceId,
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2)
        };

        _resourceRepository.GetByIdAsync(resourceId).Returns((Resource)null);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Resource with ID {resourceId} not found");
    }

    [Fact]
    public async Task Handle_WithUnavailableResource_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);
        
        var command = new CreateReservationCommand
        {
            ResourceId = resourceId,
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            StartTime = startTime,
            EndTime = endTime
        };

        var resource = new Resource("Test Resource", "Test Description");
        
        _resourceRepository.GetByIdAsync(resourceId).Returns(resource);
        _resourceRepository.IsResourceAvailableAsync(resourceId, startTime, endTime).Returns(false);

        // Act & Assert
        Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Resource is not available for the requested time period");
    }
}