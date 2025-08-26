using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LibraryManagement.API.Services
{
    public class OverdueNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueNotificationService> _logger;

        public OverdueNotificationService(IServiceProvider serviceProvider, ILogger<OverdueNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue Notification Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Checking for loans due soon...");
                    await CheckLoansDueSoon();

                    // Wait for 24 hours before running again
                    await Task.Delay(TimeSpan.FromHours(2), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // This is expected when the application is shutting down.
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while checking for overdue books.");
                    // Wait for 5 minutes before trying again to avoid spamming logs on persistent errors.
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }

            _logger.LogInformation("Overdue Notification Service is stopping.");
        }

        private async Task CheckLoansDueSoon()
        {
            // We create a new "scope" to get our DbContext. This is the correct way to use
            // services inside a long-running singleton like a BackgroundService.
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var today = DateTime.UtcNow.Date;
                var tomorrow = today.AddDays(1);

                // Find all active loans that are due today or tomorrow
                var loansDueSoon = await dbContext.Loans
                    .Include(l => l.Book)
                    .Where(l => l.ReturnDate == null &&
                                (l.DueDate.Date == today || l.DueDate.Date == tomorrow))
                    .ToListAsync();

                foreach (var loan in loansDueSoon)
                {
                    string message;
                    if (loan.DueDate.Date == today)
                    {
                        message = $"Reminder: Your book '{loan.Book.Title}' is due today!";
                    }
                    else // Due tomorrow
                    {
                        message = $"Reminder: Your book '{loan.Book.Title}' is due tomorrow.";
                    }

                    // Check if a similar notification already exists to avoid duplicates
                    var existingNotification = await dbContext.Notifications
                        .AnyAsync(n => n.UserId == loan.ApplicationUserId && n.Message == message);

                    if (!existingNotification)
                    {
                        var notification = new Notification
                        {
                            UserId = loan.ApplicationUserId,
                            Message = message,
                            Url = "/my-books"
                        };
                        dbContext.Notifications.Add(notification);
                    }
                }

                await dbContext.SaveChangesAsync();
                _logger.LogInformation($"Processed {loansDueSoon.Count} loans due soon.");
            }
        }
    }
}