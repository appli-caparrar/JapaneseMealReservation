using Microsoft.AspNetCore.Mvc;
using JapaneseMealReservation.Models;
using Microsoft.EntityFrameworkCore;
using JapaneseMealReservation.AppData;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using JapaneseMealReservation.Services;
using Order = JapaneseMealReservation.Models.Order;
using JapaneseMealReservation.ViewModels;
using TimeZoneConverter;


namespace JapaneseMealReservation.Controllers
{
    public class ReservationController : Controller
    {
        private readonly AppDbContext dbContext;
        private readonly SqlServerDbContext sqlServerDbContext;
        private readonly MailService mailService;

        public ReservationController(AppDbContext dbContext, SqlServerDbContext sqlServerDbContext, MailService mailService)
        {
            this.dbContext = dbContext;
            this.sqlServerDbContext = sqlServerDbContext;
            this.mailService = mailService;
        }

        public IActionResult MealReservation()
        {
            DateTime utcToday = DateTime.UtcNow.Date;

            // Get the start of the current week (Sunday)
            int diff = (7 + (utcToday.DayOfWeek - DayOfWeek.Sunday)) % 7;
            DateTime startOfWeek = utcToday.AddDays(-diff); // Sunday (start of week)

            // End of the week: Saturday 23:59:59.9999999
            DateTime endOfWeek = startOfWeek.AddDays(7).AddTicks(-1); // Includes entire Saturday

            // Query menus within the week
            var weeklyMenus = dbContext.Menus
                .Where(m => m.AvailabilityDate >= startOfWeek &&
                            m.AvailabilityDate <= endOfWeek &&
                            m.IsAvailable)
                .OrderBy(m => m.AvailabilityDate)
                .ToList();

            return View(weeklyMenus);
        }

        [HttpPost]
        public async Task<IActionResult> MealReservation(Order order)
        {
            if (!ModelState.IsValid)
            {
                // Return to the form view with validation errors
                return View(order);
            }

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order placed successfully!";

            return RedirectToAction("Reservation");
        }


        //[HttpGet]
        //[AllowAnonymous]
        //public IActionResult GetEmployeeInfo(string employeeId)
        //{
        //    var employee = dbContext.Users
        //        .Where(e => e.EmployeeId == employeeId)
        //        .Select(e => new
        //        {
        //            firstName = e.FirstName,
        //            lastName = e.LastName,
        //            section = e.Section,
        //            cutomerType = e.EmployeeType
        //        })
        //        .FirstOrDefault();

        //    if (employee == null)
        //        return NotFound();

        //    return Json(employee);
        //}

        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetEmployeeById(string id)
        {
            var user = dbContext.Users
            .FirstOrDefault(u => u.EmployeeId != null && u.EmployeeId.ToLower() == id.ToLower());


            if (user == null)
                return Json(new { success = false, message = "Employee record not found. This employee may not be registered in the system." });

            return Json(new
            {
                success = true,
                firstName = user.FirstName,
                lastName = user.LastName,
                section = user.Section,
                email = user.Email,
                customerType = user.EmployeeType,
            });
        }


        //public IActionResult AdvanceOrdering()
        //{
        //    return View();
        //}


        [Authorize]
        public IActionResult AdvanceOrdering()
        {
            // Extract custom claims
            var employeeId = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId")?.Value;
            var firstName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var lastName = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var section = User.Claims.FirstOrDefault(c => c.Type == "Section")?.Value;
            var employeeType = User.Claims.FirstOrDefault(c => c.Type == "EmployeeType")?.Value;

            // Build anonymous object to pass via ViewBag
            ViewBag.EmployeeData = new
            {
                EmployeeId = employeeId,
                FirstName = firstName,
                LastName = lastName,
                Section = section,
                EmployeeType = employeeType
            };

            return View();
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public JsonResult SaveAdvanceOrder([FromBody] AdvanceOrder model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        var errors = ModelState.Values
        //            .SelectMany(v => v.Errors)
        //            .Select(e => e.ErrorMessage)
        //            .ToList();

        //        return Json(new { success = false, message = "Validation failed", errors });
        //    }


        //    // Use local time (PH timezone)
        //    var currentPHTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
        //        TimeZoneInfo.FindSystemTimeZoneById("Singapore")); // UTC+8

        //    model.ReferenceNumber = $"ORD-{currentPHTime:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

        //    dbContext.AdvanceOrders.Add(model);
        //    dbContext.SaveChanges();

        //    return Json(new { success = true, message = "Reservation successful!", reference = model.ReferenceNumber });
        //}

        public async Task<string> GenerateTokenLinkAsync(string employeeId)
        {
            var token = new AccessToken
            {
                Token = Guid.NewGuid(),
                EmployeeId = employeeId,
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };

            dbContext.AccessTokens.Add(token);
            await dbContext.SaveChangesAsync();

            // Generate base link
            var link = Url.Action(
                action: "OrderSummary",
                controller: "Order",
                values: new { token = token.Token },
                protocol: Request.Scheme,
                host: Request.Host.Value
            );

            // Prepend the virtual folder (PathBase) like "/JapaneseMeal"
            var appPath = Request.PathBase.HasValue ? Request.PathBase.Value : "";
            return link;
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveAdvanceOrder([FromBody] AdvanceOrder model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, message = "Validation failed", errors });
            }

            // Use local time (PH timezone)
            var currentPHTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("Singapore")); // UTC+8

            model.ReferenceNumber = $"ORD-{currentPHTime:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

            dbContext.AdvanceOrders.Add(model);
            await dbContext.SaveChangesAsync();

            // Get employee details
            var employee = await dbContext.Users.FirstOrDefaultAsync(u => u.EmployeeId == model.EmployeeId);

            var tokenLink = await GenerateTokenLinkAsync(model.EmployeeId);

            if (employee != null && !string.IsNullOrWhiteSpace(employee.Email))
            {

                string subject = $"🍱 Order Confirmation - {model.ReferenceNumber} on {model.ReservationDate:yyyy-MM-dd}";
                string body = $@"
                <table width='100%' cellpadding='0' cellspacing='0' border='0' style='font-family: Arial, sans-serif; background-color: #f9f9f9; padding: 20px;'>
                    <tr>
                        <td align='center'>
                            <table width='600' cellpadding='0' cellspacing='0' border='0' style='background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 8px rgba(0,0,0,0.1);'>
                                <tr>
                                    <td style='background-color: #2c3e50; padding: 20px; color: #ffffff; text-align: center;'>
                                        <h2 style='margin: 0;'>Japanese Meal Reservation</h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 30px;'>
                                        <h3 style='color: #333;'>Hello {model.FirstName},</h3>
                                        <p style='font-size: 16px; color: #555;'>Your meal order has been <strong>successfully placed</strong>. Here are the details:</p>
                                        <table width='100%' cellpadding='0' cellspacing='0' style='margin-top: 20px;'>
                                            <tr><td><strong>Reference #:</strong></td><td style='padding: 8px 0;'>{model.ReferenceNumber}</td></tr>
                                            <tr style='background-color: #f5f5f5;'><td style='padding: 8px 0;'><strong>Menu:</strong></td><td style='padding: 8px 0;'>{model.MenuType ?? "N/A"}</td></tr>
                                            <tr><td style='padding: 8px 0;'><strong>Quantity:</strong></td><td style='padding: 8px 0;'>{model.Quantity}</td></tr>
                                            <tr style='background-color: #f5f5f5;'><td style='padding: 8px 0;'><strong>Date:</strong></td><td style='padding: 8px 0;'>{model.ReservationDate:yyyy-MM-dd}</td></tr>
                                            <tr><td style='padding: 8px 0;'><strong>Meal Time:</strong></td><td style='padding: 8px 0;'>{model.MealTime}</td></tr>
                                        </table>
                                        <div style='background-color: #27ae60; margin: 30px 0; padding:6px 10px; border-radius: 25px; text-align: center;'>
                                            <a href='{tokenLink}' target='_blank' style='color: #fff; text-decoration: none; display: inline-block; font-weight: bold;'>View Order Summary</a>
                                        </div>
                                        <p style='margin-top: 30px; font-size: 16px; color: #444;'>Thank you for using our service.<br/></p>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='background-color: #ecf0f1; padding: 15px; text-align: center; font-size: 12px; color: #777;'>
                                        © 2025 - BIPH - Japanese Meal Reservation
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>";

                try
                {
                    await mailService.SendEmailAsync(employee.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Email Error] {ex.Message}");
                }
            }

            return Json(new { success = true, message = "Reservation successful!", reference = model.ReferenceNumber });
        }


        //[HttpGet]
        //public async Task<IActionResult> ExpatAdvanceReservation(int month, int year)
        //{
        //    if (month < 1 || month > 12)
        //    {
        //        month = DateTime.Now.Month;
        //    }

        //    if (year < 1)
        //    {
        //        year = DateTime.Now.Year;
        //    }

        //    var daysInMonth = DateTime.DaysInMonth(year, month);

        //    var model = new ExpatReservationViewModel();

        //    model.CurrentMonthDates = Enumerable.Range(1, daysInMonth)
        //        .Select(day => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local))
        //        .ToList();

        //    model.Users = await dbContext.Users
        //        .Where(u => u.EmployeeType == "Expat")
        //        .OrderBy(u => u.FirstName)
        //        .ToListAsync();


        //    model.WeekdayMenus = new Dictionary<int, List<string>>
        //    {
        //        { 1, new() { "Bento", "Maki" } },
        //        { 2, new() { "Bento", "Noodles" } },
        //        { 3, new() { "Bento", "Maki" } },
        //        { 4, new() { "Bento", "Curry" } },
        //        { 5, new() { "Bento", "Noodles" } },
        //        { 6, new() { "Bento" } }
        //    };

        //    model.CurrentUserId = User.FindFirst("EmployeeId")?.Value;

        //    model.SelectedOrders = await dbContext.AdvanceOrders
        //        .Where(a => a.ReservationDate.Month == month && a.ReservationDate.Year == year)
        //        .Where(a => a.CustomerType == "Expat")
        //        .Select(a => $"{a.EmployeeId}|{a.ReservationDate.ToLocalTime().Date:yyyy-MM-dd}|{a.MenuType}")
        //        .ToListAsync();

        //    model.ReservedDates = await dbContext.AdvanceOrders
        //     .Where(a => a.ReservationDate.Month == month && a.ReservationDate.Year == year)
        //     .Where(a => a.CustomerType == "Expat")
        //     .Select(a => $"{a.EmployeeId}|{a.ReservationDate.ToLocalTime().Date:yyyy-MM-dd}")
        //     .Distinct()
        //     .ToListAsync();

        //    model.ReservedOrders = await dbContext.AdvanceOrders
        //      .Where(a => a.ReservationDate.Month == month && a.ReservationDate.Year == year)
        //      .Where(a => a.EmployeeId != null && a.MenuType != null)
        //      .Where(a => a.CustomerType == "Expat")
        //      .ToDictionaryAsync(
        //         a => $"{a.EmployeeId}|{a.ReservationDate.ToLocalTime().Date:yyyy-MM-dd}|{a.ReferenceNumber}",
        //          a => a.MenuType!
        //      );

        //    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        //        return PartialView("_ExpatReservationTable", model); // partial only on AJAX

        //    return View(model); // full view on normal page load
        //}



    [HttpGet]
    public async Task<IActionResult> ExpatAdvanceReservation(int month, int year)
    {
        // Set defaults if invalid values
        if (month < 1 || month > 12)
            month = DateTime.Now.Month;

        if (year < 1)
            year = DateTime.Now.Year;

        // Convert to Philippine timezone
        TimeZoneInfo phTimeZone = TZConvert.GetTimeZoneInfo("Asia/Manila");
        DateTime nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var model = new ExpatReservationViewModel();

        model.CurrentMonthDates = Enumerable.Range(1, daysInMonth)
            .Select(day => new DateTime(year, month, day))
            .ToList();

        // Get users of type Expat
        model.Users = await dbContext.Users
            .Where(u => u.EmployeeType == "Expat")
            .OrderBy(u => u.FirstName)
            .ToListAsync();

        // Define menu per weekday
        model.WeekdayMenus = new Dictionary<int, List<string>>
        {
            { 1, new() { "Bento", "Maki" } },
            { 2, new() { "Bento", "Noodles" } },
            { 3, new() { "Bento", "Maki" } },
            { 4, new() { "Bento", "Curry" } },
            { 5, new() { "Bento", "Noodles" } },
            { 6, new() { "Bento" } }
        };

        // Set current user ID from claims
        model.CurrentUserId = User.FindFirst("EmployeeId")?.Value;

        // Query reservation data
        var advanceOrders = dbContext.AdvanceOrders
            .Where(a => a.ReservationDate.Month == month && a.ReservationDate.Year == year)
            .Where(a => a.CustomerType == "Expat");

        var ordersList = await advanceOrders
            .Select(a => new
            {
                a.EmployeeId,
                a.ReservationDate,
                a.MenuType,
                a.ReferenceNumber
            }).ToListAsync();

        // Convert reservation dates to PH format
        model.SelectedOrders = ordersList
            .Select(a => $"{a.EmployeeId}|{TimeZoneInfo.ConvertTimeFromUtc(a.ReservationDate, phTimeZone):yyyy-MM-dd}|{a.MenuType}")
            .ToList();

        model.ReservedDates = ordersList
            .Select(a => $"{a.EmployeeId}|{TimeZoneInfo.ConvertTimeFromUtc(a.ReservationDate, phTimeZone):yyyy-MM-dd}")
            .Distinct()
            .ToList();

        model.ReservedOrders = ordersList
            .Where(a => a.EmployeeId != null && a.MenuType != null)
            .ToDictionary(
                a => $"{a.EmployeeId}|{TimeZoneInfo.ConvertTimeFromUtc(a.ReservationDate, phTimeZone):yyyy-MM-dd}|{a.ReferenceNumber}",
                a => a.MenuType!
            );

        // Use built-in extension to detect AJAX requests
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" || Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return PartialView("_ExpatReservationTable", model);

        return View(model);
    }



    [HttpPost]
        public async Task<IActionResult> ExpatAdvanceReservation(ExpatReservationViewModel model)
        {
            if (model.SelectedOrders != null && !string.IsNullOrEmpty(model.MealTime))
            {
                var timePH = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Singapore Standard Time"); // UTC+8

                foreach (var item in model.SelectedOrders)
                {
                    var parts = item.Split('|');
                    if (parts.Length != 3) continue;

                    var employeeId = parts[0];
                    var date = DateTime.Parse(parts[1]);
                    var menu = parts[2];

                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
                    if (user == null) continue;

                    var referenceNumber = $"ORD-{timePH:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

                    var existingOrder = await dbContext.AdvanceOrders.FirstOrDefaultAsync(o =>
                        o.EmployeeId == user.EmployeeId &&
                        o.ReservationDate.Date == date.Date &&
                        o.MealTime == model.MealTime &&
                        o.MenuType == menu // important to avoid skipping different menus
                    );

                    if (existingOrder != null)
                    {
                        continue;
                    }

                    var order = new AdvanceOrder
                    {
                        EmployeeId = user.EmployeeId,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Section = user.Section,
                        Quantity = 1,
                        ReservationDate = date,
                        MealTime = model.MealTime,
                        MenuType = menu,
                        ReferenceNumber = referenceNumber,
                        CustomerType = user.EmployeeType,
                        Status = "Pending"
                    };

                    dbContext.AdvanceOrders.Add(order);
                }

                await dbContext.SaveChangesAsync();
                ViewBag.ShowSuccessAlert = true;
            }
            else
            {
                ViewBag.ShowErrorAlert = true;
            }

            // Rebuild model after saving ✅

            var today = DateTime.Today;
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            model.CurrentMonthDates = Enumerable.Range(1, daysInMonth)
                .Select(day => new DateTime(today.Year, today.Month, day)).ToList();

            model.Users = await dbContext.Users
                .Where(u => u.EmployeeType == "Expat")
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            model.WeekdayMenus = new Dictionary<int, List<string>>
            {
                { 1, new() { "Bento", "Maki" } },
                { 2, new() { "Bento", "Noodles" } },
                { 3, new() { "Bento", "Maki" } },
                { 4, new() { "Bento", "Curry" } },
                { 5, new() { "Bento", "Noodles" } },
                { 6, new() { "Bento" } }
            };

            // ✅ RELOAD ALL EXISTING ORDERS for the month and MealTime
            var reservationStart = new DateTime(today.Year, today.Month, 1);
            var reservationEnd = reservationStart.AddMonths(1);

            var allOrders = await dbContext.AdvanceOrders
                .Where(o => o.ReservationDate >= reservationStart && o.ReservationDate < reservationEnd)
                .Where(a => a.CustomerType == "Expat")
                .ToListAsync();

            model.SelectedOrders = allOrders
                .Select(o => $"{o.EmployeeId}|{o.ReservationDate:yyyy-MM-dd}|{o.MenuType}")
                .ToList();

            model.ReservedDates = await dbContext.AdvanceOrders
              .Where(a => a.ReservationDate.Month == today.Month && a.ReservationDate.Year == today.Year)
              .Where(a => a.CustomerType == "Expat")
              .Select(a => $"{a.EmployeeId}|{a.ReservationDate.ToLocalTime().Date:yyyy-MM-dd}")
              .Distinct()
              .ToListAsync();

            model.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return View(model);
        }



    }
}