using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TicketingSystem.Infrastructure.BackgroundServices;

public class ReservationExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationExpirationWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public ReservationExpirationWorker(
        IServiceProvider serviceProvider,
        ILogger<ReservationExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationExpirationWorker is starting.");

        using PeriodicTimer timer = new PeriodicTimer(_checkInterval);

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing expired reservations.");
            }
        }

        _logger.LogInformation("ReservationExpirationWorker is stopping.");
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken stoppingToken)
    {
        await Task.CompletedTask;
    }
}
