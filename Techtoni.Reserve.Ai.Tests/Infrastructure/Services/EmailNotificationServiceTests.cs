using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Techtoni.Reserve.Ai.Infrastructure.Services;

namespace Techtoni.Reserve.Ai.Tests.Infrastructure.Services;

public class EmailNotificationServiceTests
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly EmailNotificationService _service;

    public EmailNotificationServiceTests()
    {
        _logger = Substitute.For<ILogger<EmailNotificationService>>();
        _service = new EmailNotificationService(_logger);
    }

    [Fact]
    public async Task SendReservationConfirmationAsync_ShouldLogInformation()
    {
        // Arrange
        var reservationId = Guid.NewGuid();
        var customerName = "John Doe";
        var customerEmail = "john.doe@example.com";
        var startTime = DateTime.UtcNow.AddHours(1);
        var endTime = startTime.AddHours(2);

        // Act
        await _service.SendReservationConfirmationAsync(
            reservationId,
            customerName,
            customerEmail,
            startTime,
            endTime);

        // Assert
        _logger.Received(1).Log(
            Arg.Is<LogLevel>(level => level == LogLevel.Information),
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains(reservationId.ToString()) &&
                               o.ToString().Contains(customerName) &&
                               o.ToString().Contains(customerEmail)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }
}