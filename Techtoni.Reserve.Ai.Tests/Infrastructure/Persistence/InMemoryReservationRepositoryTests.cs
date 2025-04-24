using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Infrastructure.Persistence;

namespace Techtoni.Reserve.Ai.Tests.Infrastructure.Persistence;

public class InMemoryReservationRepositoryTests
{
    private readonly InMemoryReservationRepository _repository;

    public InMemoryReservationRepositoryTests()
    {
        _repository = new InMemoryReservationRepository();
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnReservation()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var reservation = new Reservation(
            resourceId,
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));

        await _repository.AddAsync(reservation);

        // Act
        var result = await _repository.GetByIdAsync(reservation.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(reservation);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistingId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllReservations()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var reservation1 = new Reservation(
            resourceId,
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));

        var reservation2 = new Reservation(
            resourceId,
            "Jane Smith",
            "jane.smith@example.com",
            DateTime.UtcNow.AddHours(4),
            DateTime.UtcNow.AddHours(6));

        await _repository.AddAsync(reservation1);
        await _repository.AddAsync(reservation2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(reservation1);
        result.Should().Contain(reservation2);
    }

    [Fact]
    public async Task GetByResourceIdAsync_ShouldReturnMatchingReservations()
    {
        // Arrange
        var resourceId1 = Guid.NewGuid();
        var resourceId2 = Guid.NewGuid();
        
        var reservation1 = new Reservation(
            resourceId1,
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));

        var reservation2 = new Reservation(
            resourceId2,
            "Jane Smith",
            "jane.smith@example.com",
            DateTime.UtcNow.AddHours(4),
            DateTime.UtcNow.AddHours(6));

        var reservation3 = new Reservation(
            resourceId1,
            "Alice Johnson",
            "alice.johnson@example.com",
            DateTime.UtcNow.AddHours(7),
            DateTime.UtcNow.AddHours(9));

        await _repository.AddAsync(reservation1);
        await _repository.AddAsync(reservation2);
        await _repository.AddAsync(reservation3);

        // Act
        var result = await _repository.GetByResourceIdAsync(resourceId1);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(reservation1);
        result.Should().Contain(reservation3);
        result.Should().NotContain(reservation2);
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldReturnOverlappingReservations()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        
        var reservation1 = new Reservation(  // 1-3
            resourceId,
            "John Doe",
            "john.doe@example.com",
            now.AddHours(1),
            now.AddHours(3));

        var reservation2 = new Reservation(  // 4-6
            resourceId,
            "Jane Smith",
            "jane.smith@example.com",
            now.AddHours(4),
            now.AddHours(6));

        var reservation3 = new Reservation(  // 7-9
            resourceId,
            "Alice Johnson",
            "alice.johnson@example.com",
            now.AddHours(7),
            now.AddHours(9));

        await _repository.AddAsync(reservation1);
        await _repository.AddAsync(reservation2);
        await _repository.AddAsync(reservation3);

        // Act - search range 2-5 should overlap with reservations 1 and 2
        var result = await _repository.GetByDateRangeAsync(now.AddHours(2), now.AddHours(5));

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(reservation1);
        result.Should().Contain(reservation2);
        result.Should().NotContain(reservation3);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingReservation_ShouldUpdateReservation()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var reservation = new Reservation(
            resourceId,
            "John Doe",
            "john.doe@example.com",
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddHours(3));

        await _repository.AddAsync(reservation);
        
        // Modify the reservation status
        reservation.Confirm();

        // Act
        await _repository.UpdateAsync(reservation);
        var updatedReservation = await _repository.GetByIdAsync(reservation.Id);

        // Assert
        updatedReservation.Should().NotBeNull();
        updatedReservation.Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldReturnTrue()
    {
        // Act
        var result = await _repository.SaveChangesAsync();

        // Assert
        result.Should().BeTrue();
    }
}