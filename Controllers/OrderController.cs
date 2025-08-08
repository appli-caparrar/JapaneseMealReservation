using JapaneseMealReservation.AppData;
using Microsoft.AspNetCore.Mvc;
using JapaneseMealReservation.Models;
using Microsoft.EntityFrameworkCore;
using JapaneseMealReservation.Services;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;

namespace JapaneseMealReservation.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext dbContext;
        private readonly MailService mailService;
        private readonly ILogger<OrderController> logger;

        public OrderController(AppDbContext dbContext, MailService mailService, ILogger<OrderController> logger)
        {
            this.dbContext = dbContext;
            this.mailService = mailService;
            this.logger = logger;
        }

        public IActionResult Orders()
        {
            return View();
        }

        //[HttpPost]
        //public async Task<IActionResult> PlaceOrder(Order order)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new { success = false, message = "Invalid data." });
        //    }

        //    // Ensure ReservationDate is set and UTC
        //    if (order.ReservationDate == DateTime.MinValue)
        //    {
        //        return BadRequest(new { success = false, message = "Reservation date is required." });
        //    }

        //    order.ReservationDate = DateTime.SpecifyKind(order.ReservationDate, DateTimeKind.Utc);

        //    // Generate Order Number
        //    var today = order.ReservationDate.Date;
        //    int dailyCount = await dbContext.Orders.CountAsync(o => o.ReservationDate.Date == today);
        //    string sequence = (dailyCount + 1).ToString("D4");
        //    order.OrderNumber = $"ORD-{today:yyyyMMdd}-{sequence}";

        //    dbContext.Orders.Add(order);
        //    await dbContext.SaveChangesAsync();

        //    return Ok(new { success = true, orderNumber = order.OrderNumber });
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
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid order data." });
                }

                if (order.ReservationDate == DateTime.MinValue)
                {
                    return BadRequest(new { success = false, message = "Reservation date is required." });
                }

                // For ReservationDate (DateTime) — this is valid
                if (order.ReservationDate.Kind == DateTimeKind.Unspecified)
                {
                    order.ReservationDate = DateTime.SpecifyKind(order.ReservationDate, DateTimeKind.Utc);
                }

                // For MealTime (TimeSpan?) — cannot check Kind, just check HasValue if needed
                if (!order.MealTime.HasValue)
                {
                    return BadRequest(new { success = false, message = "Meal time is required." });
                }

                // If you plan to combine ReservationDate + MealTime into a full DateTime:
                DateTime mealDateTime = order.ReservationDate.Date + order.MealTime.Value;
                if (mealDateTime.Kind == DateTimeKind.Unspecified)
                {
                    mealDateTime = DateTime.SpecifyKind(mealDateTime, DateTimeKind.Utc);
                }

                // Generate a unique reference number (e.g., ORD-20250605-XYZ123)
                string refNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
                order.ReferenceNumber = refNumber;

                dbContext.Orders.Add(order);
                await dbContext.SaveChangesAsync();

                // Get employee email
                var employee = await dbContext.Users
                    .FirstOrDefaultAsync(e => e.EmployeeId == order.EmployeeId);

                var tokenLink = await GenerateTokenLinkAsync(order.EmployeeId);
                 
                if (employee != null && !string.IsNullOrWhiteSpace(employee.Email))
                {
                    string subject = $"🍱 Order Confirmation - {order.ReferenceNumber} on {order.ReservationDate:yyyy-MM-dd}";
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
                                            <h3 style='color: #333;'>Hello {order.FirstName},</h3>
                                            <p style='font-size: 16px; color: #555;'>Your meal order has been <strong>successfully placed</strong>. Here are the details:</p>
                                            <table width='100%' cellpadding='0' cellspacing='0' style='margin-top: 20px;'>
                                                <tr>
                                                    <td><strong>Reference #:</strong></td>
                                                    <td style='padding: 8px 0;'>{order.ReferenceNumber}</td>
                                                </tr>
                                                <tr style='background-color: #f5f5f5;'>
                                                    <td style='padding: 8px 0;'><strong>Menu:</strong></td>
                                                    <td style='padding: 8px 0;'>{order.OrderName ?? "N/A"}</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0;'><strong>Quantity:</strong></td>
                                                    <td style='padding: 8px 0;'>{order.Quantity}</td>
                                                </tr>
                                                <tr style='background-color: #f5f5f5;'>
                                                    <td style='padding: 8px 0;'><strong>Date:</strong></td>
                                                    <td style='padding: 8px 0;'>{order.ReservationDate:yyyy-MM-dd}</td>
                                                </tr>
                                                <tr>
                                                    <td style='padding: 8px 0;'><strong>Meal Time:</strong></td>
                                                    <td style='padding: 8px 0;'>{order.MealTime}</td>
                                                </tr>
                                            </table>

                                            <!-- ✅ Order Summary Link Button -->
                                            <div style='background-color: #27ae60; margin: 30px 0; padding:6px 10px; border-radius: 25px; text-align: center;'>
                                                <a href='{tokenLink}' target='_blank' style='color: #fff; text-decoration: none; display: inline-block; font-weight: bold;'>
                                                    View Order Summary
                                                </a>
                                            </div>

                                            <p style='margin-top: 30px; font-size: 16px; color: #444;'>
                                                Thank you for using our service.<br/>
                                            </p>
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
                        logger.LogError(ex, "Failed to send order confirmation email to EmployeeId: {EmployeeId}", order.EmployeeId);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Order placed successfully.",
                    referenceNumber = order.ReferenceNumber
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error while placing order");
                return StatusCode(500, new { success = false, message = "An internal server error occurred." });
            }
        }


        // GET: /Order/BentoOrderList
        [HttpGet]
        public async Task<IActionResult> BentoOrderList()
        {

            // Use Philippine time (UTC+8) if needed
            var today = DateTime.Today; // Use DateTime.UtcNow.Date if needed
            var tomorrow = today.AddDays(1);

            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "bento"
                    && o.ReservationDate == today)
                .OrderByDescending(o => o.ReservationDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> MakiOrderList()
        {
            // Use Philippine time (UTC+8) if needed
            var today = DateTime.Today; // Use DateTime.UtcNow.Date if needed
            var tomorrow = today.AddDays(1);

            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "maki"
                    && o.ReservationDate >= today
                    && o.ReservationDate < tomorrow)
                .OrderByDescending(o => o.ReservationDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> CurryOrderList()
        {
            // Use Philippine time (UTC+8) if needed
            var today = DateTime.Today; // Use DateTime.UtcNow.Date if needed
            var tomorrow = today.AddDays(1);

            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "curry"
                    && o.ReservationDate >= today
                    && o.ReservationDate < tomorrow)
                .OrderByDescending(o => o.ReservationDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> NoodlesOrderList()
        {
            // Use Philippine time (UTC+8) if needed
            var today = DateTime.Today; // Use DateTime.UtcNow.Date if needed
            var tomorrow = today.AddDays(1);

            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "noodles"
                    && o.ReservationDate >= today
                    && o.ReservationDate < tomorrow)
                .OrderByDescending(o => o.ReservationDate)
                .ToListAsync();

            return View(orders);
        }
        
        public async Task<IActionResult> BreakfastOrderList()
        {
            // Use Philippine time (UTC+8) if needed
            var today = DateTime.Today; // Use DateTime.UtcNow.Date if needed
            var tomorrow = today.AddDays(1);

            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "breakfast"
                    && o.ReservationDate >= today
                    && o.ReservationDate < tomorrow)
                .OrderByDescending(o => o.ReservationDate)
                .ToListAsync();

            return View(orders);
        }



        //public IActionResult OrderSummary()
        //{
        //    var employeeId = User.FindFirst("EmployeeId")?.Value;

        //    if (string.IsNullOrEmpty(employeeId))
        //    {
        //        return Unauthorized(); // Or redirect to login
        //    }

        //    var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        //    var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
        //    var todayPHStartUtc = TimeZoneInfo.ConvertTimeToUtc(
        //        new DateTime(nowPH.Year, nowPH.Month, nowPH.Day, 0, 0, 0),
        //        phTimeZone);

        //    var orderSummaries = dbContext.OrderSummaryView
        //        .Where(x => x.EmployeeId == employeeId &&
        //                    x.ReservationDate >= todayPHStartUtc)
        //        .ToList();

        //    return View(orderSummaries);
        //}

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> OrderSummary(Guid? token)
        {
            // If token is present, validate and skip login
            if (token.HasValue)
            {
                var access = await dbContext.AccessTokens
                    .FirstOrDefaultAsync(a => a.Token == token && a.ExpiresAt > DateTime.UtcNow);

                if (access != null)
                {
                    var employeeId = access.EmployeeId;

                    var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
                    var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
                    var todayPHStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                        new DateTime(nowPH.Year, nowPH.Month, nowPH.Day, 0, 0, 0), phTimeZone);

                    var orderSummaries = dbContext.OrderSummaryView
                        .Where(x => x.EmployeeId == employeeId &&
                                    x.ReservationDate >= todayPHStartUtc)
                        .ToList();

                    return View(orderSummaries);
                }

                // Show token expired/invalid message
                return View("OrderSummaryTokenExpired"); 
            }

            // Otherwise, use authenticated session
            var employeeIdFromLogin = User.FindFirst("EmployeeId")?.Value;

            if (string.IsNullOrEmpty(employeeIdFromLogin))
                return Unauthorized();

            var orders = await dbContext.OrderSummaryView
                .Where(x => x.EmployeeId == employeeIdFromLogin)
                .ToListAsync();

            return View(orders);
        }




        [HttpGet]
        public JsonResult GetOrdersForCalendar()
        {
            var employeeId = User.FindFirst("EmployeeId")?.Value;

            if (string.IsNullOrEmpty(employeeId))
            {
                return Json(new { success = false, message = "Unauthorized access." });
            }

            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

            var events = dbContext.OrderSummaryView
                .Where(o => o.EmployeeId == employeeId)
                .ToList() // Fetch first, then convert to local time
                .Select(o => new
                {
                    title = o.MenuType + " x" + o.Quantity,
                    start = TimeZoneInfo.ConvertTimeFromUtc(o.ReservationDate, phTimeZone).ToString("yyyy-MM-dd")
                })
                .ToList();

            return Json(events);
        }

        [HttpPost]
        public IActionResult UpdateQuantity(string ReferenceNumber, int Quantity)
        {
            var source = dbContext.CombineOrders
                .Where(x => x.ReferenceNumber == ReferenceNumber)
                .Select(x => x.Source)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(source))
            {
                TempData["UpdateStatus"] = "error";
                TempData["UpdateMessage"] = "Order not found.";
                return RedirectToAction("OrderSummary");
            }

            if (source == "Order")
            {
                var order = dbContext.Orders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);
                if (order != null)
                {
                    order.Quantity = Quantity;
                }
            }
            else if (source == "AdvanceOrder")
            {
                var advOrder = dbContext.AdvanceOrders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);
                if (advOrder != null)
                {
                    advOrder.Quantity = Quantity;
                }
            }

            dbContext.SaveChanges();
            TempData["UpdateStatus"] = "success";
            TempData["UpdateMessage"] = "Quantity updated successfully.";
            return RedirectToAction("OrderSummary");
        }

        //[HttpPost]
        //public IActionResult CancelOrder(string ReferenceNumber)
        //{
        //    var source = dbContext.CombineOrders
        //        .Where(x => x.ReferenceNumber == ReferenceNumber)
        //        .Select(x => x.Source)
        //        .FirstOrDefault();

        //    if (string.IsNullOrEmpty(source))
        //    {
        //        TempData["UpdateStatus"] = "error";
        //        TempData["UpdateMessage"] = "Order not found.";
        //        return RedirectToAction("OrderSummary");
        //    }

        //    if (source == "Order")
        //    {
        //        var order = dbContext.Orders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);
        //        if (order != null)
        //        {
        //            order.Status = "Cancelled"; // ✅ Update status
        //        }
        //    }
        //    else if (source == "AdvanceOrder")
        //    {
        //        var advOrder = dbContext.AdvanceOrders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);
        //        if (advOrder != null)
        //        {
        //            advOrder.Status = "Cancelled"; // ✅ Update status
        //        }
        //    }

        //    dbContext.SaveChanges();

        //    TempData["UpdateStatus"] = "success";
        //    TempData["UpdateMessage"] = "Order cancelled successfully.";
        //    return RedirectToAction("OrderSummary");
        //}

        [HttpPost]
        public async Task<IActionResult> CancelOrder(string ReferenceNumber)
        {
            var source = dbContext.CombineOrders
                .Where(x => x.ReferenceNumber == ReferenceNumber)
                .Select(x => x.Source)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(source))
            {
                TempData["UpdateStatus"] = "error";
                TempData["UpdateMessage"] = "Order not found.";
                return RedirectToAction("OrderSummary");
            }

            string email = null;
            string name = null;
            DateTime? date = null;
            TimeSpan? time = null;

            if (source == "Order")
            {
                var order = dbContext.Orders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);
                if (order != null)
                {
                    order.Status = "Cancelled";

                    var user = dbContext.Users.FirstOrDefault(u => u.EmployeeId == order.EmployeeId);
                    if (user != null)
                    {
                        email = user.Email;
                        name = order.FirstName;
                        date = order.ReservationDate;
                        time = order.MealTime;
                    }
                }
            }
            else if (source == "AdvanceOrder")
            {
                var advOrder = dbContext.AdvanceOrders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);

                if (advOrder != null)
                {
                    advOrder.Status = "Cancelled";

                    var user = dbContext.Users.FirstOrDefault(u => u.EmployeeId == advOrder.EmployeeId);
                    if (user != null)
                    {
                        email = user.Email;
                        name = advOrder.FirstName;
                        date = advOrder.ReservationDate;
                        if (!string.IsNullOrWhiteSpace(advOrder.MealTime))
                        {
                            if (TimeSpan.TryParse(advOrder.MealTime, out var parsedTime))
                            {
                                time = parsedTime;
                            }
                        }
                    }
                }
            }

            await dbContext.SaveChangesAsync();

            // Send cancellation email
            if (!string.IsNullOrWhiteSpace(email))
            {
                string subject = $"❌ Meal Order Cancelled - {ReferenceNumber}";
                string body = $@"
                <table width='100%' cellpadding='0' cellspacing='0' border='0' style='font-family: Arial, sans-serif; background-color: #f9f9f9; padding: 20px;'>
                    <tr>
                        <td align='center'>
                            <table width='600' cellpadding='0' cellspacing='0' border='0' style='background-color: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 8px rgba(0,0,0,0.1);'>
                                <tr>
                                    <td style='background-color: #c0392b; padding: 20px; color: #ffffff; text-align: center;'>
                                        <h2 style='margin: 0;'>Order Cancelled</h2>
                                    </td>
                                </tr>
                                <tr>
                                    <td style='padding: 30px;'>
                                        <h3 style='color: #333;'>Hi {name},</h3>
                                        <p style='font-size: 16px; color: #555;'>Your meal reservation has been <strong>cancelled</strong>. Please see the details below:</p>
                                        <table width='100%' cellpadding='0' cellspacing='0' style='margin-top: 20px;'>
                                            <tr>
                                                <td><strong>Reference #:</strong></td>
                                                <td style='padding: 8px 0;'>{ReferenceNumber}</td>
                                            </tr>
                                            <tr style='background-color: #f5f5f5;'>
                                                <td style='padding: 8px 0;'><strong>Reservation Date:</strong></td>
                                                <td style='padding: 8px 0;'>{(date.HasValue ? date.Value.ToString("yyyy-MM-dd") : "N/A")}</td>
                                            </tr>
                                            <tr>
                                                <td style='padding: 8px 0;'><strong>Meal Time:</strong></td>
                                                <td style='padding: 8px 0;'>{(time.HasValue ? time.Value.ToString() : "N/A")}</td>
                                            </tr>
                                        </table>
                                        <p style='margin-top: 30px; font-size: 16px; color: #444;'>If this was a mistake or you wish to reorder, please visit the japanese meal reservation again.</p>
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
                    await mailService.SendEmailAsync(email, subject, body);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send cancellation email for Reference #: {ReferenceNumber}", ReferenceNumber);
                }
            }

            TempData["UpdateStatus"] = "success";
            TempData["UpdateMessage"] = "Order cancelled successfully.";
            return RedirectToAction("OrderSummary");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTodayBentoOrders(string MenuType)
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
            var today = nowPH.Date;
            var todayPHStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                            new DateTime(nowPH.Year, nowPH.Month, nowPH.Day, 0, 0, 0),
                            phTimeZone);

            var normalizedMenuType = MenuType?.ToLower() ?? string.Empty;

            var references = dbContext.CombineOrders
             .Where(x => x.ReservationDate >= todayPHStartUtc &&
                         x.MenuType.ToLower() == "bento")
             .Select(x => new { x.ReferenceNumber, x.Source })
             .ToList();

            if (!references.Any())
            {
                return Json(new { success = false, message = "No matching orders found." });
            }

            foreach (var item in references)
            {
                if (item.Source == "Order")
                {
                    var order = dbContext.Orders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (order != null && order.Status == "Pending")
                        order.Status = "Completed";
                }
                else if (item.Source == "AdvanceOrder")
                {
                    var advOrder = dbContext.AdvanceOrders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (advOrder != null && advOrder.Status == "Pending")
                        advOrder.Status = "Completed";
                }
            }

            dbContext.SaveChanges();

            return Json(new { success = true, message = $"All {MenuType} orders marked as Completed." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTodayCurryOrders(string MenuType)
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
            var today = nowPH.Date;
            var todayPHStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                            new DateTime(nowPH.Year, nowPH.Month, nowPH.Day, 0, 0, 0),
                            phTimeZone);

            var normalizedMenuType = MenuType?.ToLower() ?? string.Empty;

            var references = dbContext.CombineOrders
             .Where(x => x.ReservationDate >= todayPHStartUtc &&
                         x.MenuType.ToLower() == "curry")
             .Select(x => new { x.ReferenceNumber, x.Source })
             .ToList();

            if (!references.Any())
            {
                return Json(new { success = false, message = "No matching orders found." });
            }

            foreach (var item in references)
            {
                if (item.Source == "Order")
                {
                    var order = dbContext.Orders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (order != null && order.Status == "Pending")
                        order.Status = "Completed";
                }
                else if (item.Source == "AdvanceOrder")
                {
                    var advOrder = dbContext.AdvanceOrders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (advOrder != null && advOrder.Status == "Pending")
                        advOrder.Status = "Completed";
                }
            }

            dbContext.SaveChanges();

            return Json(new { success = true, message = $"All {MenuType} orders marked as Completed." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTodayMakiOrders(string MenuType)
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
            var today = nowPH.Date;
            var todayPHStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                            new DateTime(nowPH.Year, nowPH.Month, nowPH.Day, 0, 0, 0),
                            phTimeZone);

            var normalizedMenuType = MenuType?.ToLower() ?? string.Empty;

            var references = dbContext.CombineOrders
             .Where(x => x.ReservationDate >= todayPHStartUtc &&
                         x.MenuType.ToLower() == "maki")
             .Select(x => new { x.ReferenceNumber, x.Source })
             .ToList();

            if (!references.Any())
            {
                return Json(new { success = false, message = "No matching orders found." });
            }

            foreach (var item in references)
            {
                if (item.Source == "Order")
                {
                    var order = dbContext.Orders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (order != null && order.Status == "Pending")
                        order.Status = "Completed";
                }
                else if (item.Source == "AdvanceOrder")
                {
                    var advOrder = dbContext.AdvanceOrders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (advOrder != null && advOrder.Status == "Pending")
                        advOrder.Status = "Completed";
                }
            }

            dbContext.SaveChanges();

            return Json(new { success = true, message = $"All {MenuType} orders marked as Completed." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTodayNoodlesOrders(string MenuType)
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
            var today = nowPH.Date;
            var todayPHStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                            new DateTime(nowPH.Year, nowPH.Month, nowPH.Day, 0, 0, 0),
                            phTimeZone);

            var normalizedMenuType = MenuType?.ToLower() ?? string.Empty;

            var references = dbContext.CombineOrders
             .Where(x => x.ReservationDate >= todayPHStartUtc &&
                         x.MenuType.ToLower() == "noodles")
             .Select(x => new { x.ReferenceNumber, x.Source })
             .ToList();

            if (!references.Any())
            {
                return Json(new { success = false, message = "No matching orders found." });
            }

            foreach (var item in references)
            {
                if (item.Source == "Order")
                {
                    var order = dbContext.Orders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (order != null && order.Status == "Pending")
                        order.Status = "Completed";
                }
                else if (item.Source == "AdvanceOrder")
                {
                    var advOrder = dbContext.AdvanceOrders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (advOrder != null && advOrder.Status == "Pending")
                        advOrder.Status = "Completed";
                }
            }

            dbContext.SaveChanges();

            return Json(new { success = true, message = $"All {MenuType} orders marked as Completed." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteTodayBreakfastOrders(string MenuType)
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
            var today = nowPH.Date;
            var todayPHStartUtc = TimeZoneInfo.ConvertTimeToUtc(
                            new DateTime(nowPH.Year, nowPH.Month, nowPH.Day, 0, 0, 0),
                            phTimeZone);

            var normalizedMenuType = MenuType?.ToLower() ?? string.Empty;

            var references = dbContext.CombineOrders
             .Where(x => x.ReservationDate >= todayPHStartUtc &&
                         x.MenuType.ToLower() == "breakfast")
             .Select(x => new { x.ReferenceNumber, x.Source })
             .ToList();

            if (!references.Any())
            {
                return Json(new { success = false, message = "No matching orders found." });
            }

            foreach (var item in references)
            {
                if (item.Source == "Order")
                {
                    var order = dbContext.Orders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (order != null && order.Status == "Pending")
                        order.Status = "Completed";
                }
                else if (item.Source == "AdvanceOrder")
                {
                    var advOrder = dbContext.AdvanceOrders.FirstOrDefault(o => o.ReferenceNumber == item.ReferenceNumber);
                    if (advOrder != null && advOrder.Status == "Pending")
                        advOrder.Status = "Completed";
                }
            }

            dbContext.SaveChanges();

            return Json(new { success = true, message = $"All {MenuType} orders marked as Completed." });
        }


    }
}
