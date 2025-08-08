using JapaneseMealReservation.AppData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using JapaneseMealReservation.Models;
using JapaneseMealReservation.ViewModels;


namespace JapaneseMealReservation.Services
{
    public class MealReminderJob
    {
        private readonly IServiceProvider _serviceProvider;

        public MealReminderJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Run()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var mailService = scope.ServiceProvider.GetRequiredService<MailService>();

            var nowUtc = DateTime.UtcNow;
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, phTimeZone);
            var rangeStart = nowPH.AddMinutes(29);
            var rangeEnd = nowPH.AddMinutes(31);

            var upcomingOrders = db.OrderSummaryView
                .Where(o => o.ReservationDate.Date == nowPH.Date && o.Status == "Pending")
                .AsEnumerable() // Switch to client-side LINQ to allow parsing
                .Where(o =>
                {
                    if (TimeSpan.TryParse(o.MealTime, out var mealTime))
                    {
                        var mealDateTime = o.ReservationDate.Date.Add(mealTime);
                        return mealDateTime >= rangeStart && mealDateTime <= rangeEnd;
                    }
                    return false;
                })
                .ToList();

            foreach (var order in upcomingOrders)
            {
                var name = $"{order.FirstName} {order.LastName}";
                var menuType = order.MenuType;

                TimeSpan mealTimeSpan;
                if (TimeSpan.TryParse(order.MealTime, out mealTimeSpan))
                {
                    var mealDateTime = order.ReservationDate.Date.Add(mealTimeSpan);

                    string emailHtml = $@"
                    <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; background-color: #f9f9f9; color: #333; }}
                                .container {{
                                    background-color: #ffffff;
                                    border-radius: 8px;
                                    padding: 20px;
                                    max-width: 600px;
                                    margin: auto;
                                    box-shadow: 0 0 10px rgba(0,0,0,0.1);
                                }}
                                .header {{ font-size: 24px; font-weight: bold; color: #2c3e50; margin-bottom: 20px; }}
                                .info {{ margin-bottom: 15px; font-size: 16px; }}
                                .footer {{ margin-top: 30px; font-size: 12px; color: #888; }}
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='header'>🍱 Meal Reminder</div>
                                <div class='info'>Hello <strong>{name}</strong>,</div>
                                <div class='info'>This is a friendly reminder that your <strong>{menuType}</strong> meal is scheduled at <strong>{mealDateTime:hh\\:mm tt}</strong>.</div>
                                <div class='info'>Please ensure you are ready to receive your meal on time.</div>
                                <div class='footer'>This is an automated message from the Japanese Meal Reservation System.</div>
                            </div>
                        </body>
                    </html>";

                    await mailService.SendEmailAsync(order.email, "🍱 Meal Reminder", emailHtml);
                }
            }

            Console.WriteLine($"[ReminderJob] Running at {nowPH:hh:mm:ss tt}"); //for checking only
        }

    }
}