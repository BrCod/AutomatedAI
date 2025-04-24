using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Techtoni.Reserve.Ai.Domain.Entities;
using Techtoni.Reserve.Ai.Domain.Interfaces;
using Techtoni.Reserve.Ai.Infrastructure.Persistence;

namespace Techtoni.Reserve.Ai.Tests.Infrastructure.Persistence;

public class InMemoryResourceRepositoryTests
{
    private readonly IReservationRepository _reservationRepository;
    private readonly InMemoryResourceRepository _repository;

    public InMemoryResourceRepositoryTests()
    {
        _reservationRepository = Substitute.For<IReservationRepository>();
        _repository = new InMemoryResourceRepository(_reservationRepository);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnResource()
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "A spacious meeting room");
        await _repository.AddAsync(resource);

        // Act
        var result = await _repository.GetByIdAsync(resource.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(resource);
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
    public async Task GetAllAsync_ShouldReturnAllResources()
    {
        // Arrange
        var resource1 = new Resource("Meeting Room A", "A spacious meeting room");
        var resource2 = new Resource("Conference Room B", "Large conference room");
        
        await _repository.AddAsync(resource1);
        await _repository.AddAsync(resource2);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(resource1);
        result.Should().Contain(resource2);
    }

    [Fact]
    public async Task GetAvailableAsync_ShouldReturnOnlyAvailableResources()
    {
        // Arrange
        var resource1 = new Resource("Meeting Room A", "A spacious meeting room");
        var resource2 = new Resource("Conference Room B", "Large conference room");
        var resource3 = new Resource("Office C", "Small office");
        
        // Make resource2 unavailable
        resource2.SetAvailability(false);
        
        await _repository.AddAsync(resource1);
        await _repository.AddAsync(resource2);
        await _repository.AddAsync(resource3);

        // Act
        var result = await _repository.GetAvailableAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(resource1);
        result.Should().Contain(resource3);
        result.Should().NotContain(resource2);
    }

    [Fact]
    public async Task IsResourceAvailableAsync_WithNonExistingResource_ShouldReturnFalse()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // Act
        var result = await _repository.IsResourceAvailableAsync(resourceId, startTime, endTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsResourceAvailableAsync_WithUnavailableResource_ShouldReturnFalse()
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "A spacious meeting room");
        resource.SetAvailability(false);
        await _repository.AddAsync(resource);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // Act
        var result = await _repository.IsResourceAvailableAsync(resource.Id, startTime, endTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsResourceAvailableAsync_WithNoOverlappingReservations_ShouldReturnTrue()
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "A spacious meeting room");
        await _repository.AddAsync(resource);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // No overlapping reservations
        _reservationRepository.GetByResourceIdAsync(resource.Id).Returns(new List<Reservation>().AsEnumerable());

        // Act
        var result = await _repository.IsResourceAvailableAsync(resource.Id, startTime, endTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsResourceAvailableAsync_WithOverlappingReservations_ShouldReturnFalse()
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "A spacious meeting room");
        await _repository.AddAsync(resource);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // Create an overlapping reservation
        var reservation = new Reservation(
            resource.Id,
            "John Doe",
            "john.doe@example.com",
            startTime.AddMinutes(-30),
            endTime.AddMinutes(30));

        _reservationRepository.GetByResourceIdAsync(resource.Id).Returns(new List<Reservation> { reservation });

        // Act
        var result = await _repository.IsResourceAvailableAsync(resource.Id, startTime, endTime);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsResourceAvailableAsync_WithCancelledOverlappingReservation_ShouldReturnTrue()
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "A spacious meeting room");
        await _repository.AddAsync(resource);

        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // Create an overlapping but cancelled reservation
        var reservation = new Reservation(
            resource.Id,
            "John Doe",
            "john.doe@example.com",
            startTime.AddMinutes(-30),
            endTime.AddMinutes(30));
        
        // Cancel the reservation
        reservation.Cancel();

        _reservationRepository.GetByResourceIdAsync(resource.Id).Returns(new List<Reservation> { reservation });

        // Act
        var result = await _repository.IsResourceAvailableAsync(resource.Id, startTime, endTime);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingResource_ShouldUpdateResource()
    {
        // Arrange
        var resource = new Resource("Meeting Room A", "Original description");
        await _repository.AddAsync(resource);
        
        // Update resource properties
        resource.Update("Meeting Room A Updated", "Updated description");
        resource.SetAvailability(false);

        // Act
        await _repository.UpdateAsync(resource);
        var updatedResource = await _repository.GetByIdAsync(resource.Id);

        // Assert
        updatedResource.Should().NotBeNull();
        updatedResource.Name.Should().Be("Meeting Room A Updated");
        updatedResource.Description.Should().Be("Updated description");
        updatedResource.IsAvailable.Should().BeFalse();
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