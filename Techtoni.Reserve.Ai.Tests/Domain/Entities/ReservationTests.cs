using System;
using FluentAssertions;
using Techtoni.Reserve.Ai.Domain.Entities;

namespace Techtoni.Reserve.Ai.Tests.Domain.Entities;

public class ReservationTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateReservation()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = "john.doe@example.com";
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // Act
        var reservation = new Reservation(
            resourceId,
            customerName,
            customerEmail,
            startTime,
            endTime);

        // Assert
        reservation.Should().NotBeNull();
        reservation.Id.Should().NotBeEmpty();
        reservation.ResourceId.Should().Be(resourceId);
        reservation.CustomerName.Should().Be(customerName);
        reservation.CustomerEmail.Should().Be(customerEmail);
        reservation.StartTime.Should().Be(startTime);
        reservation.EndTime.Should().Be(endTime);
        reservation.Status.Should().Be(ReservationStatus.Pending);
        reservation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        reservation.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyResourceId_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceId = Guid.Empty;
        var customerName = "John Doe";
        var customerEmail = "john.doe@example.com";
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // Act & Assert
        Action act = () => new Reservation(
            resourceId,
            customerName,
            customerEmail,
            startTime,
            endTime);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Resource ID cannot be empty*")
            .WithParameterName("resourceId");
    }

    [Theory]
    [InlineData("", "john.doe@example.com")]
    [InlineData(" ", "john.doe@example.com")]
    [InlineData(null, "john.doe@example.com")]
    [InlineData("John Doe", "")]
    [InlineData("John Doe", " ")]
    [InlineData("John Doe", null)]
    public void Constructor_WithInvalidCustomerInfo_ShouldThrowArgumentException(string customerName, string customerEmail)
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // Act & Assert
        Action act = () => new Reservation(
            resourceId,
            customerName,
            customerEmail,
            startTime,
            endTime);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithEndTimeBeforeStartTime_ShouldThrowArgumentException()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = "john.doe@example.com";
        var startTime = DateTime.UtcNow.AddHours(2);
        var endTime = DateTime.UtcNow.AddHours(1); // End time before start time

        // Act & Assert
        Action act = () => new Reservation(
            resourceId,
            customerName,
            customerEmail,
            startTime,
            endTime);

        act.Should().Throw<ArgumentException>()
            .WithMessage("End time must be after start time*")
            .WithParameterName("endTime");
    }

    [Fact]
    public void Confirm_ShouldChangeStatusToConfirmed()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var reservation = new Reservation(
            resourceId,
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));

        // Act
        reservation.Confirm();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.UpdatedAt.Should().NotBeNull();
        reservation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Cancel_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var reservation = new Reservation(
            resourceId,
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));

        // Act
        reservation.Cancel();

        // Assert
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
        reservation.UpdatedAt.Should().NotBeNull();
        reservation.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}