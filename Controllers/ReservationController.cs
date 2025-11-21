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
using System;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;


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
                                            <tr style='background-color: #f5f5f5;'><td style='padding: 8px 0;'><strong>Menu:</strong></td><td style='padding: 8px 0;'>{model.MenuType ?? "N/A"}</td></tr
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
        public async Task<IActionResult> ExpatLunchAdvanceReservation(int month = 0, int year = 0, string? employeeId = null)
        {
            var phTimeZone = TZConvert.GetTimeZoneInfo("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);

            month = month < 1 || month > 12 ? nowPH.Month : month;
            year = year < 1 ? nowPH.Year : year;

            var daysInMonth = DateTime.DaysInMonth(year, month);

            var model = new ExpatReservationViewModel
            {
                SelectedMonth = month,
                SelectedYear = year,
                CurrentMonthDates = Enumerable.Range(1, daysInMonth)
                    .Select(d => new DateTime(year, month, d))
                    .ToList(),
                CurrentUserId = User.FindFirst("EmployeeId")?.Value,
                SelectedEmployeeId = employeeId
            };

            // Load all Expats and Managers
            var allUsers = await dbContext.Users
                .Where(u => u.EmployeeType == "Expat" || (u.Position ?? "").Contains("Manager"))
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            model.AllExpatsAndManagers = allUsers;

            // Apply filter if employeeId selected
            model.Users = string.IsNullOrEmpty(employeeId)
                ? allUsers
                : allUsers.Where(u => u.EmployeeId == employeeId).ToList();

            // Weekday menus
            model.WeekdayMenus = new Dictionary<int, List<string>>
    {
        { 1, new() { "Bento", "Maki" } },
        { 2, new() { "Bento", "Noodles" } },
        { 3, new() { "Bento", "Maki" } },
        { 4, new() { "Bento", "Curry" } },
        { 5, new() { "Bento", "Noodles" } },
        { 6, new() { "Bento" } }
    };

            // Load existing lunch orders for month
            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var orders = await dbContext.AdvanceOrders
                .Where(o => o.ReservationDate >= startOfMonth && o.ReservationDate < endOfMonth)
                .Where(o => o.MenuType != "Breakfast")
                .ToListAsync();

            // Map reservations for UI
            model.SelectedOrders = orders
                .Select(o => $"{o.EmployeeId}|{TimeZoneInfo.ConvertTimeFromUtc(o.ReservationDate, phTimeZone):yyyy-MM-dd}|{o.MenuType}")
                .ToList();

            model.ReservedDates = orders
                .Select(o => $"{o.EmployeeId}|{TimeZoneInfo.ConvertTimeFromUtc(o.ReservationDate, phTimeZone):yyyy-MM-dd}")
                .Distinct()
                .ToList();

            model.ReservedOrders = orders
                .ToDictionary(
                    o => $"{o.EmployeeId}|{TimeZoneInfo.ConvertTimeFromUtc(o.ReservationDate, phTimeZone):yyyy-MM-dd}|{o.ReferenceNumber}",
                    o => o.MenuType
                );

            model.ReservedOrdersStatus = orders
                .GroupBy(o => $"{o.EmployeeId}|{TimeZoneInfo.ConvertTimeFromUtc(o.ReservationDate, phTimeZone):yyyy-MM-dd}")
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g.Select(x => x.Status).Distinct())
                );

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_ExpatLunchReservationTable", model);

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ExpatLunchAdvanceReservation(ExpatReservationViewModel model)
        {
            var currentUserId = User.FindFirst("EmployeeId")?.Value;
            var phTimeZone = TZConvert.GetTimeZoneInfo("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
            var todayPH = nowPH.Date;

            model.MealTime ??= "12:00"; // default noon

            // Load all Expats & Managers
            var allUsers = await dbContext.Users
                .Where(u => u.EmployeeType == "Expat" || (u.Position ?? "").Contains("Manager"))
                .ToListAsync();

            model.AllExpatsAndManagers = allUsers;

            // Filter by selected employee if needed
            model.Users = string.IsNullOrEmpty(model.SelectedEmployeeId)
                ? allUsers
                : allUsers.Where(u => u.EmployeeId == model.SelectedEmployeeId).ToList();

            model.CurrentUserId = currentUserId;

            if (model.SelectedOrders == null || !model.SelectedOrders.Any())
            {
                ViewBag.ShowErrorAlert = true;
                return PartialView("_ExpatLunchReservationTable", model);
            }

            var startOfMonth = new DateTime(nowPH.Year, nowPH.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var existingOrders = await dbContext.AdvanceOrders
                .Where(o => o.ReservationDate >= startOfMonth && o.ReservationDate < endOfMonth)
                .Where(o => o.MealTime == model.MealTime)
                .ToListAsync();

            var processedSet = new HashSet<string>();

            foreach (var item in model.SelectedOrders.Distinct())
            {
                var parts = item.Split('|');
                if (parts.Length != 3) continue;

                var employeeId = parts[0];
                if (!DateTime.TryParse(parts[1], out var date)) continue;
                var menu = parts[2];
                date = TimeZoneInfo.ConvertTimeToUtc(date.Date, phTimeZone);

                var uniqueKey = $"{employeeId}|{date:yyyy-MM-dd}|{menu}";
                if (!processedSet.Add(uniqueKey)) continue;

                var user = allUsers.FirstOrDefault(u => u.EmployeeId == employeeId);
                if (user == null || user.EmployeeId != currentUserId) continue;
                if (date < todayPH) continue;

                var existingOrder = existingOrders.FirstOrDefault(o =>
                    o.EmployeeId == user.EmployeeId &&
                    o.ReservationDate == date &&
                    o.MealTime == model.MealTime);

                if (existingOrder != null)
                {
                    if (!string.Equals(existingOrder.MenuType, menu, StringComparison.OrdinalIgnoreCase))
                    {
                        existingOrder.MenuType = menu;
                        existingOrder.Status = "Pending";
                        dbContext.AdvanceOrders.Update(existingOrder);
                    }
                }
                else
                {
                    dbContext.AdvanceOrders.Add(new AdvanceOrder
                    {
                        EmployeeId = user.EmployeeId,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Section = user.Section,
                        Quantity = 1,
                        ReservationDate = date,
                        MealTime = model.MealTime,
                        MenuType = menu,
                        ReferenceNumber = $"ORD-{nowPH:yyyyMMdd}-{Guid.NewGuid():N}".Substring(0, 20).ToUpper(),
                        CustomerType = user.EmployeeType,
                        Status = "Pending"
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            ViewBag.ShowSuccessAlert = true;

            // Reload model for partial view
            return await ExpatLunchAdvanceReservation(nowPH.Month, nowPH.Year, model.SelectedEmployeeId) as PartialViewResult;
        }




        [HttpGet]
        public async Task<IActionResult> ExpatBreakfastAdvanceReservation(int month, int year, string? employeeId)
        {
            // Default month/year handling
            if (month < 1 || month > 12) month = DateTime.Now.Month;
            if (year < 1) year = DateTime.Now.Year;

            var phTimeZone = TZConvert.GetTimeZoneInfo("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
            var daysInMonth = DateTime.DaysInMonth(year, month);

            // Initial model
            var model = new ExpatReservationViewModel
            {
                CurrentMonthDates = Enumerable.Range(1, daysInMonth)
                    .Select(day => new DateTime(year, month, day))
                    .ToList(),
                CurrentUserId = User.FindFirst("EmployeeId")?.Value,
                SelectedEmployeeId = employeeId
            };

            // Load all expats & managers for dropdown
            var allUsers = await dbContext.Users
                .Where(u => u.EmployeeType == "Expat" || (u.Position ?? "").Contains("Manager"))
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            model.AllExpatsAndManagers = allUsers
                .Select(u => new User
                {
                    EmployeeId = u.EmployeeId,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Position = u.Position
                })
                .ToList();

            // Apply dropdown filtering for table
            model.Users = string.IsNullOrEmpty(employeeId)
                ? allUsers  // Show all
                : allUsers.Where(u => u.EmployeeId == employeeId).ToList(); // Show only selected

            // Date range
            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            // Load orders for breakfast
            var ordersList = await (
                from o in dbContext.AdvanceOrders
                join u in dbContext.Users on o.EmployeeId equals u.EmployeeId
                where o.ReservationDate >= startOfMonth && o.ReservationDate < endOfMonth
                where u.EmployeeType == "Expat" || (u.Position ?? "").Contains("Manager")
                where o.MenuType == "Breakfast"
                select new
                {
                    o.EmployeeId,
                    o.ReservationDate,
                    o.MenuType,
                    o.ReferenceNumber,
                    o.Status
                }
            ).ToListAsync();

            // Prepare model
            model.SelectedOrders = ordersList
                .Select(a => $"{a.EmployeeId}|{a.ReservationDate:yyyy-MM-dd}|{a.MenuType}")
                .ToList();

            model.ReservedDates = ordersList
                .Select(a => $"{a.EmployeeId}|{TimeZoneInfo.ConvertTime(a.ReservationDate, phTimeZone):yyyy-MM-dd}")
                .Distinct()
                .ToList();

            model.ReservedOrders = ordersList
                .Where(a => a.EmployeeId != null && a.MenuType != null)
                .ToDictionary(
                    a => $"{a.EmployeeId}|{TimeZoneInfo.ConvertTime(a.ReservationDate, phTimeZone):yyyy-MM-dd}|{a.ReferenceNumber}",
                    a => a.MenuType!
                );

            model.ReservedOrdersStatus = ordersList
                .GroupBy(a => $"{a.EmployeeId}|{TimeZoneInfo.ConvertTime(a.ReservationDate, phTimeZone):yyyy-MM-dd}")
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g.Select(x => x.Status).Distinct())
                );

            // Return partial view for AJAX or full view
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_ExpatBreakfastReservationTable", model);

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> ExpatBreakfastAdvanceReservation(ExpatReservationViewModel model)
        {

            var currentUserId = User.FindFirst("EmployeeId")?.Value;
            var phTimeZone = TZConvert.GetTimeZoneInfo("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTime(DateTime.UtcNow, phTimeZone);
            var todayPH = nowPH.Date;


            // If dropdown filter was used, preserve it
            string selectedEmployeeId = model.SelectedEmployeeId ?? "";

            // Load all expat & manager users
            var allUsers = await dbContext.Users
                .Where(u => u.EmployeeType == "Expat" || (u.Position ?? "").Contains("Manager"))
                .ToListAsync();

            // Apply filtering if needed
            if (!string.IsNullOrEmpty(selectedEmployeeId))
            {
                // Filter only the selected employee
                model.Users = allUsers
                    .Where(u => u.EmployeeId == selectedEmployeeId)
                    .OrderBy(u => u.FirstName)
                    .ToList();
            }
            else
            {
                // No filter, show all expats + managers
                model.Users = allUsers.OrderBy(u => u.FirstName).ToList();
            }


            model.AllExpatsAndManagers = allUsers; // for dropdown
            model.CurrentUserId = currentUserId;

            // Basic validation
            if (model.SelectedOrders == null || !model.SelectedOrders.Any())
            {
                ViewBag.ShowErrorAlert = true;
                return PartialView("_ExpatBreakfastReservationTable", model);
            }

            var mealTimeInput = string.IsNullOrWhiteSpace(model.MealTime) ? "07:00" : model.MealTime.Trim();

         
            // Load all breakfast orders for current month (including Cancelled)
            var startOfMonth = new DateTime(nowPH.Year, nowPH.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            var allOrders = await dbContext.AdvanceOrders
                .Where(o => o.ReservationDate >= startOfMonth && o.ReservationDate < endOfMonth)
                .Where(o => o.MealTime == mealTimeInput)
                .ToListAsync();

            foreach (var item in model.SelectedOrders)
            {
                var parts = item.Split('|');
                if (parts.Length != 3) continue;

                var employeeId = parts[0];
                if (!DateTime.TryParse(parts[1], out var selectedDate)) continue;
                var menu = parts[2];

                // Manila local date
                var phDate = TimeZoneInfo.ConvertTime(selectedDate.Date, phTimeZone);

                var user = allUsers.FirstOrDefault(u => u.EmployeeId == employeeId);
                if (user == null || user.EmployeeId != currentUserId) continue; // Only current user

                if (phDate < todayPH) continue; // Prevent past dates

                // Check if there’s already an order for this user + date + time
                var existingOrder = allOrders.FirstOrDefault(o =>
                    o.EmployeeId == user.EmployeeId &&
                    TimeZoneInfo.ConvertTime(o.ReservationDate, phTimeZone).Date == phDate.Date &&
                    o.MealTime == mealTimeInput);

                if (existingOrder != null)
                {
                    // If previously cancelled, reactivate and update menu
                    existingOrder.MenuType = menu;
                    existingOrder.Status = "Pending";
                    dbContext.AdvanceOrders.Update(existingOrder);
                }
                else
                {
                    // Add new order
                    dbContext.AdvanceOrders.Add(new AdvanceOrder
                    {
                        EmployeeId = user.EmployeeId,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Section = user.Section,
                        Quantity = 1,
                        ReservationDate = TimeZoneInfo.ConvertTimeToUtc(phDate, phTimeZone),
                        MealTime = mealTimeInput,
                        MenuType = menu,
                        ReferenceNumber = $"ORD-{nowPH:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                        CustomerType = user.EmployeeType,
                        Status = "Pending"
                    });
                }
            }

            await dbContext.SaveChangesAsync();
            ViewBag.ShowSuccessAlert = true;

            // Reload data for view
            var daysInMonth = DateTime.DaysInMonth(nowPH.Year, nowPH.Month);
            model.CurrentMonthDates = Enumerable.Range(1, daysInMonth)
                .Select(day => new DateTime(nowPH.Year, nowPH.Month, day))
                .ToList();

            model.Users = allUsers.OrderBy(u => u.FirstName).ToList();

            // Reload all orders again
            allOrders = await dbContext.AdvanceOrders
                .Where(o => o.ReservationDate >= startOfMonth && o.ReservationDate < endOfMonth)
                .Where(o => o.MealTime == mealTimeInput)
                .ToListAsync();

            var activeOrders = allOrders.Where(o => !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).ToList();

            model.SelectedOrders = activeOrders
                .Select(o =>
                {
                    var phDate = TimeZoneInfo.ConvertTime(o.ReservationDate, phTimeZone).Date;
                    return $"{o.EmployeeId}|{phDate:yyyy-MM-dd}|{o.MenuType}";
                })
                .ToList();

            model.ReservedDates = activeOrders
                .Select(o =>
                {
                    var phDate = TimeZoneInfo.ConvertTime(o.ReservationDate, phTimeZone).Date;
                    return $"{o.EmployeeId}|{phDate:yyyy-MM-dd}";
                })
                .Distinct()
                .ToList();

           model.ReservedOrders = activeOrders
                .Where(o => o.EmployeeId != null && o.MenuType != null)
                .ToDictionary(
                    o =>
                    {
                        var phDate = TimeZoneInfo.ConvertTime(o.ReservationDate, phTimeZone).Date;
                        return $"{o.EmployeeId}|{phDate:yyyy-MM-dd}|{o.ReferenceNumber}";
                    },
                    o =>  o.MenuType!
                );

            model.ReservedOrdersStatus = allOrders
                .GroupBy(o =>
                {
                    var phDate = TimeZoneInfo.ConvertTime(o.ReservationDate, phTimeZone).Date;
                    return $"{o.EmployeeId}|{phDate:yyyy-MM-dd}";
                })
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g.Select(x => x.Status).Distinct())
                );

            model.CurrentUserId = currentUserId;

            return PartialView("_ExpatBreakfastReservationTable", model);
        }


        [HttpPost]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequest request)
        {
            var ReferenceNumber = request.ReferenceNumber?.Trim();
            if (string.IsNullOrEmpty(ReferenceNumber))
                return Json(new { success = false, message = "Invalid Reference Number." });

            var advOrder = await dbContext.AdvanceOrders
                .FirstOrDefaultAsync(x => x.ReferenceNumber.ToLower() == ReferenceNumber.ToLower());
            var order = await dbContext.Orders
                .FirstOrDefaultAsync(x => x.ReferenceNumber.ToLower() == ReferenceNumber.ToLower());

            if (advOrder == null && order == null)
                return Json(new { success = false, message = "Order not found." });

            if (advOrder != null) advOrder.Status = "Cancelled";
            if (order != null) order.Status = "Cancelled";

            await dbContext.SaveChangesAsync();
            return Json(new { success = true, referenceNumber = ReferenceNumber });
        }

        private List<DateTime> LoadMonthDates()
        {
            var now = DateTime.Now;
            int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);

            var dates = new List<DateTime>();
            for (int day = 1; day <= daysInMonth; day++)
            {
                dates.Add(new DateTime(now.Year, now.Month, day));
            }

            return dates;
        }


        private List<string> LoadReservedDates()
        {
            return dbContext.AdvanceOrders
                .Select(o => $"{o.EmployeeId}|{o.ReservationDate:yyyy-MM-dd}")
                .ToList();
        }


        //public async Task<IActionResult> FilterExpatsById(string employeeId, int month, int year)
        //{
        //    // Rebuild full model
        //    var mainModel = await ExpatBreakfastAdvanceReservation(month, year) as ViewResult;
        //    var model = mainModel.Model as ExpatReservationViewModel;

        //    // Apply filter
        //    if (!string.IsNullOrEmpty(employeeId))
        //        model.Users = model.Users.Where(u => u.EmployeeId == employeeId).ToList();

        //    return PartialView("_ExpatBreakfastReservationTable", model);
        //}





    }
}