using JapaneseMealReservation.AppData;
using Microsoft.AspNetCore.Mvc;
using JapaneseMealReservation.Models;
using Microsoft.EntityFrameworkCore;
using JapaneseMealReservation.Services;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Authorization;
using DocumentFormat.OpenXml.Drawing.Charts;
using JapaneseMealReservation.Migrations;
using Order = JapaneseMealReservation.Models.Order;
using JapaneseMealReservation.ViewModels;
using System.Text;
using DinkToPdf;
using Orientation = DinkToPdf.Orientation;
using DinkToPdf.Contracts;
using Rotativa.AspNetCore;

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
                                                    <td style='padding: 8px 0;'>{order.MenuType}: {order.OrderName ?? "N/A"}</td>
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

        public async Task<IActionResult> DailyOrderSummary()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var orders = await dbContext.OrderSummaryView
                .Where(o => o.ReservationDate >= today && o.ReservationDate < tomorrow)
                .OrderByDescending(o => o.ReservationDate)
                .ToListAsync();

            var model = new DailyOrderSummaryViewModel
            {
                BentoOrders = orders.Where(o => o.MenuType == "Bento").ToList(),
                BreakfastOrders = orders.Where(o => o.MenuType == "Breakfast").ToList(),
                CurryOrders = orders.Where(o => o.MenuType == "Curry").ToList(),
                MakiOrders = orders.Where(o => o.MenuType == "Maki").ToList(),
                NoodlesOrders = orders.Where(o => o.MenuType == "Noodles").ToList()
            };

            return View(model);
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
            string? employeeId;

            if (token.HasValue)
            {
                var access = await dbContext.AccessTokens
                    .FirstOrDefaultAsync(a => a.Token == token && a.ExpiresAt > DateTime.UtcNow);

                if (access == null) return View("OrderSummaryTokenExpired");
                employeeId = access.EmployeeId;
            }
            else
            {
                employeeId = User.FindFirst("EmployeeId")?.Value;
                if (string.IsNullOrEmpty(employeeId)) return Unauthorized();
            }

            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var nowPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone);
            var todayPHStartUtc = TimeZoneInfo.ConvertTimeToUtc(new DateTime(nowPH.Year, nowPH.Month, nowPH.Day), phTimeZone);

            var orders = await dbContext.OrderSummaryView
                .Where(x => x.EmployeeId == employeeId && x.ReservationDate >= todayPHStartUtc)
                .OrderBy(x => x.ReservationDate)
                .Select(x => new OrderSummaryViewModel
                {
                    ReferenceNumber = x.ReferenceNumber,
                    EmployeeId = x.EmployeeId,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Section = x.Section,
                    email = x.email,
                    CustomerType = x.CustomerType,
                    ReservationDate = x.ReservationDate,
                    MealTime = x.MealTime,
                    MenuType = x.MenuType,
                    MenuName = string.IsNullOrWhiteSpace(x.MenuName) ? "No specific menu uploaded yet" : x.MenuName,
                    Status = x.Status,
                    Quantity = x.Quantity,
                    Price = x.Price ?? 0
                })
                .ToListAsync();

            return View(orders);
        }


        [HttpGet]
        public JsonResult GetOrdersForCalendar()
        {
            var employeeId = User.FindFirst("EmployeeId")?.Value;
            if (string.IsNullOrEmpty(employeeId))
                return Json(new { success = false, message = "Unauthorized access." });

            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");

            // 1Get from DB first (simple projection only)
            var orders = dbContext.OrderSummaryView
                .Where(o => o.EmployeeId == employeeId)
                .Select(o => new
                {
                    o.MenuType,
                    o.Status,
                    o.Quantity,
                    o.ReservationDate
                })
                .ToList(); // materialize results here before using switch

            // Now process in memory (safe for switch / logic)
            var events = orders.Select(o => new
            {
                title = $"{o.MenuType} ×{o.Quantity} " +
                (o.Status == "Completed" ? "(Completed)" :
                 o.Status == "Pending" ? "(Pending)" :
                 o.Status == "Cancelled" ? "(Cancelled)" : ""),

                start = TimeZoneInfo.ConvertTimeFromUtc(o.ReservationDate, phTimeZone)
                .ToString("yyyy-MM-dd"),

                color = o.Status switch
                {
                    "Cancelled" => "gray",
                    "Completed" => "green",
                    "Pending" => "#facc15", // yellow
                    _ => "#6b7280"
                }
            }).ToList();



            return Json(events);
        }





        [HttpPost]
        public IActionResult UpdateQuantity(string ReferenceNumber, int Quantity, string OrderName, string MenuType)
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
                    order.OrderName = OrderName;
                    order.MenuType = MenuType;
                }
            }
            else if (source == "AdvanceOrder")
            {
                var advOrder = dbContext.AdvanceOrders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);
                if (advOrder != null)
                {
                    advOrder.Quantity = Quantity;
                    advOrder.MenuName = OrderName;
                    advOrder.MenuType = MenuType;
                }
            }

            dbContext.SaveChanges();

            TempData["UpdateStatus"] = "success";
            TempData["UpdateMessage"] = "Order updated successfully.";
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
            string orderDetails = null;
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
                        orderDetails = $"{order.MenuType}: {order.OrderName}";
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
                        orderDetails = $"{advOrder.MenuType}: {advOrder.MenuName}";
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
                                            <tr>
                                                <td><strong>Order:</strong></td>
                                                <td style='padding: 8px 0;'>{orderDetails}</td>
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
        //[ValidateAntiForgeryToken]
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
        //[ValidateAntiForgeryToken]
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
        //[ValidateAntiForgeryToken]
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
        //[ValidateAntiForgeryToken]
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

        [HttpGet]
        public async Task<IActionResult> GetMenus(DateTime reservationDate, string menuType)
        {
            var menus = await dbContext.Menus
                .Where(m => m.MenuType == menuType && m.AvailabilityDate.HasValue)
                .ToListAsync();  // bring them into memory

            var result = menus
                .Where(m => m.AvailabilityDate.Value.Date == reservationDate.Date)
                .Select(m => new {
                    m.Id,
                    m.MenuType,
                    m.Name
                })
                .ToList();

            return Json(result);
        }

        // Download Local Orders
        [HttpGet]
        public IActionResult DownloadTodayLocalOrders()
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;

            var orders = dbContext.OrderSummaryView
                .Where(o => o.ReservationDate.Date == today &&   // no .HasValue, no .Value
                            o.CustomerType.ToLower() == "local")
                .Select(o => new
                {
                    o.MenuType,
                    o.ReferenceNumber,
                    o.EmployeeId,
                    o.FirstName,
                    o.LastName,
                    o.Section,
                    o.Quantity,
                    o.Status
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("MenuType,ReferenceNumber,EmployeeId,FirstName,LastName,Section,Quantity,Status");

            foreach (var o in orders)
            {
                sb.AppendLine($"{o.MenuType},{o.ReferenceNumber},{o.EmployeeId},{o.FirstName},{o.LastName},{o.Section},{o.Quantity},{o.Status}");
            }

            var fileName = $"LocalOrders_{today:yyyyMMdd}.csv";
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
        }


        // Download Expat Orders
        [HttpGet]
        public IActionResult DownloadTodayExpatOrders()
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;

            var orders = dbContext.OrderSummaryView
                .Where(o => o.ReservationDate.Date == today &&
                            o.CustomerType.ToLower() == "expat")
                .Select(o => new
                {
                    o.MenuType,
                    o.ReferenceNumber,
                    o.EmployeeId,
                    o.FirstName,
                    o.LastName,
                    o.Section,
                    o.Quantity,
                    o.Status
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("MenuType,ReferenceNumber,EmployeeId,FirstName,LastName,Section,Quantity,Status");

            foreach (var o in orders)
            {
                sb.AppendLine($"{o.MenuType},{o.ReferenceNumber},{o.EmployeeId},{o.FirstName},{o.LastName},{o.Section},{o.Quantity},{o.Status}");
            }

            var fileName = $"ExpatOrders_{today:yyyyMMdd}.csv";
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
        }


        public async Task<IActionResult> MakiOrdersPdf()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "maki"
                            && o.ReservationDate >= today
                            && o.ReservationDate < tomorrow)
                .OrderBy(o => o.CustomerType) // Local first, then Expat
                .ThenBy(o => o.ReservationDate)
                .ToListAsync();

            return new ViewAsPdf("MakiOrdersPdf", orders)
            {
                FileName = $"Maki_Orders_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }

        //public async Task<IActionResult> BentoOrdersPdf()
        //{
        //    var today = DateTime.Today;
        //    var tomorrow = today.AddDays(1);

        //    var orders = await dbContext.OrderSummaryView
        //        .Where(o => o.MenuType.Trim().ToLower() == "bento"
        //                    && o.ReservationDate >= today
        //                    && o.ReservationDate < tomorrow)
        //        .OrderBy(o => o.CustomerType) // Local first, then Expat
        //        .ThenBy(o => o.ReservationDate)
        //        .ToListAsync();

        //    return new ViewAsPdf("BentoOrdersPdf", orders)
        //    {
        //        FileName = $"Bento_Orders_{DateTime.Now:yyyyMMdd}.pdf",
        //        PageSize = Rotativa.AspNetCore.Options.Size.A4,
        //        PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
        //        PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
        //    };
        //}

        public async Task<IActionResult> BentoOrdersPdf()
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var todayPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;
            var tomorrowPH = todayPH.AddDays(1);

            // Get orders for the PDF
            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "bento"
                            && o.ReservationDate >= todayPH
                            && o.ReservationDate < tomorrowPH)
                .OrderBy(o => o.CustomerType)
                .ThenBy(o => o.ReservationDate)
                .ToListAsync();

            // Mark orders as Completed (reuse your existing logic)
            var references = dbContext.CombineOrders
                .Where(x => x.ReservationDate >= todayPH && x.MenuType.ToLower() == "bento")
                .Select(x => new { x.ReferenceNumber, x.Source })
                .ToList();

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

            // 3️⃣ Return the PDF
            return new ViewAsPdf("BentoOrdersPdf", orders)
            {
                FileName = $"Bento_Orders_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }


        public async Task<IActionResult> NoodlesOrdersPdf()
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var todayPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;
            var tomorrowPH = todayPH.AddDays(1);

            // Get orders for the PDF
            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "noodles"
                            && o.ReservationDate >= todayPH
                            && o.ReservationDate < tomorrowPH)
                .OrderBy(o => o.CustomerType)
                .ThenBy(o => o.ReservationDate)
                .ToListAsync();

            // Mark orders as Completed (reuse your existing logic)
            var references = dbContext.CombineOrders
                .Where(x => x.ReservationDate >= todayPH && x.MenuType.ToLower() == "noodles")
                .Select(x => new { x.ReferenceNumber, x.Source })
                .ToList();

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

            return new ViewAsPdf("NoodlesOrdersPdf", orders)
            {
                FileName = $"Noodles_Orders_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }

        public async Task<IActionResult> CurryOrdersPdf()
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var todayPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;
            var tomorrowPH = todayPH.AddDays(1);

            // Get orders for the PDF
            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "curry"
                            && o.ReservationDate >= todayPH
                            && o.ReservationDate < tomorrowPH)
                .OrderBy(o => o.CustomerType)
                .ThenBy(o => o.ReservationDate)
                .ToListAsync();

            // Mark orders as Completed (reuse your existing logic)
            var references = dbContext.CombineOrders
                .Where(x => x.ReservationDate >= todayPH && x.MenuType.ToLower() == "curry")
                .Select(x => new { x.ReferenceNumber, x.Source })
                .ToList();

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

            return new ViewAsPdf("CurryOrdersPdf", orders)
            {
                FileName = $"Curry_Orders_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }

        public async Task<IActionResult> BreakfastOrdersPdf()
        {
            var phTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
            var todayPH = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, phTimeZone).Date;
            var tomorrowPH = todayPH.AddDays(1);

            // Get orders for the PDF
            var orders = await dbContext.OrderSummaryView
                .Where(o => o.MenuType.Trim().ToLower() == "breakfast"
                            && o.ReservationDate >= todayPH
                            && o.ReservationDate < tomorrowPH)
                .OrderBy(o => o.CustomerType)
                .ThenBy(o => o.ReservationDate)
                .ToListAsync();

            // Mark orders as Completed (reuse your existing logic)
            var references = dbContext.CombineOrders
                .Where(x => x.ReservationDate >= todayPH && x.MenuType.ToLower() == "breakfast")
                .Select(x => new { x.ReferenceNumber, x.Source })
                .ToList();

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

            return new ViewAsPdf("BreakfastOrdersPdf", orders)
            {
                FileName = $"Breakfast_Orders_{DateTime.Now:yyyyMMdd}.pdf",
                PageSize = Rotativa.AspNetCore.Options.Size.A4,
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait,
                PageMargins = new Rotativa.AspNetCore.Options.Margins(10, 10, 10, 10)
            };
        }

    }
}
