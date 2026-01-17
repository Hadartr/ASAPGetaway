using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ASAPGetaway.DAL;
using ASAPGetaway.Services;

namespace ASAPGetaway.Services
{
    // Background service - sends trip reminders daily at midnight
    public class TripReminderService : BackgroundService
    {
        private readonly ILogger<TripReminderService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public TripReminderService(
            ILogger<TripReminderService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        // Main service loop - runs continuously
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Trip Reminder Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendRemindersAsync();

                    // Wait until next midnight
                    DateTime now = DateTime.Now;
                    DateTime midnight = now.Date.AddDays(1);
                    TimeSpan delay = midnight - now;

                    _logger.LogInformation($"⏰ Next reminder check scheduled at {midnight:yyyy-MM-dd HH:mm:ss}");
                    
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in Trip Reminder Service");
                    // If error occurs, wait 1 hour and retry
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }

            _logger.LogInformation("🛑 Trip Reminder Service stopped.");
        }

        // Send reminders to all bookings that need them today
        private async Task SendRemindersAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var bookingsDal = scope.ServiceProvider.GetRequiredService<BookingsDAL>();
                var tripsDal = scope.ServiceProvider.GetRequiredService<TripsDAL>();
                var emailService = scope.ServiceProvider.GetRequiredService<EmailService>();

                _logger.LogInformation("🔍 Checking for bookings needing reminders today...");

                // Get bookings that need reminders based on ReminderDaysBeforeDeparture
                var bookings = bookingsDal.GetBookingsNeedingRemindersToday();

                if (bookings.Count == 0)
                {
                    _logger.LogInformation("✅ No reminders to send today.");
                    return;
                }

                _logger.LogInformation($"📧 Found {bookings.Count} booking(s) needing reminders.");

                int successCount = 0;
                int failCount = 0;

                foreach (var (bookingId, tripId, userId, email) in bookings)
                {
                    try
                    {
                        var trip = tripsDal.GetTripById(tripId);
                        
                        if (trip == null)
                        {
                            _logger.LogWarning($"⚠️ Trip {tripId} not found for booking {bookingId}");
                            failCount++;
                            continue;
                        }

                        // Send reminder email
                        _logger.LogInformation($"📤 Sending reminder to {email} for trip '{trip.PackageName}'...");
                        
                        bool sent = await emailService.SendTripReminderAsync(
                            email,
                            trip.PackageName,
                            trip.StartDate
                        );

                        if (sent)
                        {
                            successCount++;
                            _logger.LogInformation($"✅ Reminder sent successfully to {email}");
                        }
                        else
                        {
                            failCount++;
                            _logger.LogWarning($"⚠️ Failed to send reminder to {email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        _logger.LogError(ex, $"❌ Error sending reminder for booking {bookingId}");
                    }
                }

                _logger.LogInformation($"📊 Reminder Summary: {successCount} sent, {failCount} failed");
            }
        }
    }
}