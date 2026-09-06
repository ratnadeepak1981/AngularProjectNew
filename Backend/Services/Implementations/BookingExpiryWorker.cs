using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CampusServicesPortal.Services.Interfaces;

namespace CampusServicesPortal.Services.Implementations
{
    public class BookingExpiryWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingExpiryWorker> _logger;

        public BookingExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<BookingExpiryWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Native Background Service initialized.");

            // Rule #12: Sweeper daemon loops explicitly every 60 seconds [0.12]
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        // 1. Process Lab Reservation Expirations [0.12]
                        // FIX: Request ILabBookingService instead of ILabService
                        var labBookingService = scope.ServiceProvider.GetRequiredService<ILabBookingService>();
                        await labBookingService.ProcessExpiredHoldsAsync();

                        // 2. Process Event Registration Expirations [0.12]
                        var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
                        await eventService.ProcessExpiredHoldsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing background sweep.");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
