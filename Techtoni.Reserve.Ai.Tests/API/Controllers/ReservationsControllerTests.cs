using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.ReturnsExtensions;
using Techtoni.Reserve.Ai.API.Controllers;
using Techtoni.Reserve.Ai.Application.Commands;
using Techtoni.Reserve.Ai.Application.Queries;
using Techtoni.Reserve.Ai.Domain.Entities;

namespace Techtoni.Reserve.Ai.Tests.API.Controllers;

public class ReservationsControllerTests
{
    private readonly IMediator _mediator;
    private readonly ReservationsController _controller;

    public ReservationsControllerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new ReservationsController(_mediator);
    }

    [Fact]
    public async Task GetReservations_ShouldReturnOkWithReservations()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(7);
        
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

        _mediator.Send(Arg.Is<GetReservationsQuery>(q => 
            q.ResourceId == resourceId && 
            q.StartDate == startDate && 
            q.EndDate == endDate), 
            Arg.Any<CancellationToken>())
            .Returns(reservations);

        // Act
        var result = await _controller.GetReservations(resourceId, startDate, endDate);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeSameAs(reservations);
    }

    [Fact]
    public async Task GetReservation_WithExistingId_ShouldReturnOkWithReservation()
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

        _mediator.Send(Arg.Is<GetReservationByIdQuery>(q => q.Id == reservationId), 
            Arg.Any<CancellationToken>())
            .Returns(reservation);

        // Act
        var result = await _controller.GetReservation(reservationId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeSameAs(reservation);
    }

    [Fact]
    public async Task GetReservation_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        
        _mediator.Send(Arg.Is<GetReservationByIdQuery>(q => q.Id == reservationId), 
            Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var result = await _controller.GetReservation(reservationId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateReservation_WithValidCommand_ShouldReturnCreatedWithId()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var command = new CreateReservationCommand
        {
            ResourceId = resourceId,
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3)
        };

        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(reservationId);

        // Act
        var result = await _controller.CreateReservation(command);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.ActionName.Should().Be(nameof(ReservationsController.GetReservation));
        createdResult.RouteValues["id"].Should().Be(reservationId);
        createdResult.Value.Should().Be(reservationId);
    }

    [Fact]
    public async Task CreateReservation_WithInvalidResource_ShouldReturnBadRequest()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var command = new CreateReservationCommand
        {
            ResourceId = resourceId,
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3)
        };

        var errorMessage = $"Resource with ID {resourceId} not found";
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Throws(new ArgumentException(errorMessage));

        // Act
        var result = await _controller.CreateReservation(command);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task CreateReservation_WithUnavailableResource_ShouldReturnConflict()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var command = new CreateReservationCommand
        {
            ResourceId = resourceId,
            CustomerName = "John Doe",
            CustomerEmail = "john.doe@example.com",
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(3)
        };

        var errorMessage = "Resource is not available for the requested time period";
        _mediator.Send(command, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException(errorMessage));

        // Act
        var result = await _controller.CreateReservation(command);

        // Assert
        result.Result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result.Result as ConflictObjectResult;
        conflictResult.Value.Should().Be(errorMessage);
    }

    [Fact]
    public async Task CheckAvailability_WithAvailableResource_ShouldReturnOkWithTrue()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);
        
        _mediator.Send(Arg.Is<CheckResourceAvailabilityQuery>(q => 
            q.ResourceId == resourceId && 
            q.StartTime == startTime && 
            q.EndTime == endTime), 
            Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _controller.CheckAvailability(resourceId, startTime, endTime);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().Be(true);
    }

    [Fact]
    public async Task CheckAvailability_WithUnavailableResource_ShouldReturnOkWithFalse()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);
        
        _mediator.Send(Arg.Is<CheckResourceAvailabilityQuery>(q => 
            q.ResourceId == resourceId && 
            q.StartTime == startTime && 
            q.EndTime == endTime), 
            Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _controller.CheckAvailability(resourceId, startTime, endTime);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().Be(false);
    }

    [Fact]
    public async Task CheckAvailability_WithInvalidResource_ShouldReturnBadRequest()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);
        
        var errorMessage = $"Resource with ID {resourceId} not found";
        _mediator.Send(Arg.Is<CheckResourceAvailabilityQuery>(q => 
            q.ResourceId == resourceId && 
            q.StartTime == startTime && 
            q.EndTime == endTime), 
            Arg.Any<CancellationToken>())
            .Throws(new ArgumentException(errorMessage));

        // Act
        var result = await _controller.CheckAvailability(resourceId, startTime, endTime);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Value.Should().Be(errorMessage);
    }
}