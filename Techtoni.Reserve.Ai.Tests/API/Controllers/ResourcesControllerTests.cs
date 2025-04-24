using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Techtoni.Reserve.Ai.API.Controllers;
using Techtoni.Reserve.Ai.Application.Commands;
using Techtoni.Reserve.Ai.Application.Queries;
using Techtoni.Reserve.Ai.Domain.Entities;
using Xunit;

namespace Techtoni.Reserve.Ai.Tests.API.Controllers;

public class ResourcesControllerTests
{
    private readonly IMediator _mediator;
    private readonly ResourcesController _controller;

    public ResourcesControllerTests()
    {
        _mediator = Substitute.For<IMediator>();
        _controller = new ResourcesController(_mediator);
    }

    [Fact]
    public async Task GetResources_ShouldReturnOkWithResources()
    {
        // Arrange
        var resources = new List<Resource>
        {
            new Resource("Meeting Room A", "A spacious meeting room"),
            new Resource("Conference Room B", "Large conference room")
        };

        _mediator.Send(Arg.Is<GetResourcesQuery>(q => q.OnlyAvailable == false), 
            Arg.Any<CancellationToken>())
            .Returns(resources);

        // Act
        var result = await _controller.GetResources(false);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeSameAs(resources);
    }

    [Fact]
    public async Task GetAvailableResources_ShouldReturnOkWithAvailableResources()
    {
        // Arrange
        var resources = new List<Resource>
        {
            new Resource("Meeting Room A", "A spacious meeting room"),
            new Resource("Conference Room B", "Large conference room")
        };

        _mediator.Send(Arg.Is<GetResourcesQuery>(q => q.OnlyAvailable == true), 
            Arg.Any<CancellationToken>())
            .Returns(resources);

        // Act
        var result = await _controller.GetResources(true);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeSameAs(resources);
    }

    [Fact]
    public async Task GetResource_WithExistingId_ShouldReturnOkWithResource()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var resource = new Resource("Meeting Room A", "A spacious meeting room");

        _mediator.Send(Arg.Is<GetResourceByIdQuery>(q => q.Id == resourceId), 
            Arg.Any<CancellationToken>())
            .Returns(resource);

        // Act
        var result = await _controller.GetResource(resourceId);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Value.Should().BeSameAs(resource);
    }

    [Fact]
    public async Task GetResource_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        
        _mediator.Send(Arg.Is<GetResourceByIdQuery>(q => q.Id == resourceId), 
            Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var result = await _controller.GetResource(resourceId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateResource_WithValidCommand_ShouldReturnCreatedWithId()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var command = new CreateResourceCommand
        {
            Name = "Meeting Room A",
            Description = "A spacious meeting room"
        };

        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(resourceId);

        // Act
        var result = await _controller.CreateResource(command);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.ActionName.Should().Be(nameof(ResourcesController.GetResource));
        createdResult.RouteValues["id"].Should().Be(resourceId);
        createdResult.Value.Should().Be(resourceId);
    }

    [Fact]
    public async Task UpdateResource_WithValidCommand_ShouldReturnNoContent()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var command = new UpdateResourceCommand
        {
            Id = resourceId,
            Name = "Updated Meeting Room A",
            Description = "Updated description",
            IsAvailable = false
        };

        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _controller.UpdateResource(resourceId, command);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateResource_WithNonExistingId_ShouldReturnNotFound()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var command = new UpdateResourceCommand
        {
            Id = resourceId,
            Name = "Updated Meeting Room A",
            Description = "Updated description",
            IsAvailable = false
        };

        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _controller.UpdateResource(resourceId, command);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateResource_WithMismatchedIds_ShouldReturnBadRequest()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var differentId = Guid.NewGuid();
        var command = new UpdateResourceCommand
        {
            Id = differentId,
            Name = "Updated Meeting Room A",
            Description = "Updated description",
            IsAvailable = false
        };

        // Act
        var result = await _controller.UpdateResource(resourceId, command);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult.Value.Should().Be("Resource ID in the URL does not match the ID in the request body");
    }
}