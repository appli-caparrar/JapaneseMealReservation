using JapaneseMealReservation.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JapaneseMealReservation.AppData;
using JapaneseMealReservation.ViewModels;
using DocumentFormat.OpenXml.InkML;
using Microsoft.Extensions.Logging;
using JapaneseMealReservation.Services;

namespace JapaneseMealReservation.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly AppDbContext dbContext;
        private readonly MailService mailService;
        private readonly ILogger<OrderController> logger;

        public AdminController(AppDbContext dbContext, MailService mailService, ILogger<OrderController> logger)
        {
            this.dbContext = dbContext;
            this.mailService = mailService;
            this.logger = logger;
        }

        //[Authorize]
        //public IActionResult Dashboard()
        //{
        //    var menuItems = dbContext.Menus.ToList();
        //    return View(menuItems);
        //}

        [Authorize]
        public IActionResult Dashboard()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var model = new DashboardPageModel
            {
                Menus = dbContext.Menus.ToList(),
                Order = new Order(),
                TotalBentoToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "bento" &&
                                o.ReservationDate >= today && 
                                o.ReservationDate < tomorrow)
                    .Count(),



                TotalMakiToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "maki" &&
                                o.ReservationDate == today &&
                                o.ReservationDate < tomorrow)
                    .Count(),

                TotalCurryToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "curry" &&
                                o.ReservationDate == today &&
                                o.ReservationDate < tomorrow)
                    .Count(),

                TotalNoodlesToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "noodles" &&
                                o.ReservationDate == today &&
                                o.ReservationDate < tomorrow)
                    .Count(),

                TotalBreakfastToday = dbContext.OrderSummaryView
                    .Where(o => o.MenuType != null &&
                                o.MenuType.Trim().ToLower() == "breakfast" &&
                                o.ReservationDate == today &&
                                o.ReservationDate < tomorrow)
                    .Count()
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Dashboard(Login model)
        { 
            // Check if the submitted form model is valid
            if (!ModelState.IsValid)
            {
                return View(model); // Return the same view with validation messages
            }

            // Attempt to find a user in the database that matches the username and password
            var user = dbContext.Users
                .FirstOrDefault(user => user.EmployeeId == model.EmployeeId && user.Password == model.Password);

            // If no user found, display an error message and return to the login view
            if (user == null)
            {
                ViewBag.ErrorMessage = "Invalid username or password.";
                return RedirectToAction("Index", "Home"); // Stay on the login page
            }

            // Create a list of claims (user identity data), here storing the user's first name
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.FirstName ?? string.Empty)
            };

            // Create a ClaimsIdentity using the claims and specify the authentication scheme
            var identity = new ClaimsIdentity(claims, "MyCookieAuth");

            // Create a ClaimsPrincipal that holds the identity
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user by issuing the authentication cookie
            await HttpContext.SignInAsync("MyCookieAuth", principal);

            // Redirect the authenticated user to the Admin page
            return RedirectToAction("Dashboard", "Admin");
        }

        //public IActionResult MenuList()
        //{
        //    var menus = dbContext.Menus.ToList();
        //    return View(menus); // Passes the list to the view
        //}

        public IActionResult FilterMenu(DateTime? startDate, DateTime? endDate)
        {
            var query = dbContext.Menus.AsQueryable();

            // If no filter is provided, default to the current week
            if (!startDate.HasValue || !endDate.HasValue)
            {
                // Calculate start (Monday) and end (Sunday) of current week
                var today = DateTime.Today;
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                var weekStart = today.AddDays(-1 * diff).Date; // Monday
                var weekEnd = weekStart.AddDays(6).Date;       // Sunday

                startDate = weekStart;
                endDate = weekEnd;
            }

            // Apply filter
            query = query.Where(m => m.AvailabilityDate >= startDate.Value &&
                                     m.AvailabilityDate <= endDate.Value);

            var menus = query
                .OrderBy(m => m.AvailabilityDate)
                .ToList();

            var model = new DashboardPageModel
            {
                StartDate = startDate,
                EndDate = endDate,
                Menus = menus
            };

            return View("Dashboard", model);
        }



        [HttpGet]
        public IActionResult ExpatMonthlyDeduction()
        {
            var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            var expatOrders = dbContext.OrderSummaryView
                .Where(o => o.CustomerType == "Expat"
                            && o.ReservationDate >= monthStart
                            && o.ReservationDate < monthEnd
                            && o.Status == "Completed")
                .ToList();

            var grouped = expatOrders
                 .GroupBy(o => new { o.FirstName, o.LastName })
                 .Select(g => new ExpatMonthlyDeduction
                 {
                     Name = g.Key.FirstName + " " + g.Key.LastName,
                     ExpatBento = (decimal)g.Where(x => x.MenuType == "Bento").Sum(x => (x.Price ?? 0) * x.Quantity),
                     ExpatCurryRice = (decimal)g.Where(x => x.MenuType == "Curry").Sum(x => (x.Price ?? 0) * x.Quantity),
                     ExpatNoodles = (decimal)g.Where(x => x.MenuType == "Noodles").Sum(x => (x.Price ?? 0) * x.Quantity),
                     MakiRoll = (decimal)g.Where(x => x.MenuType == "Maki").Sum(x => (x.Price ?? 0) * x.Quantity),
                     Breakfast = (decimal)g.Where(x => x.MenuType == "Breakfast").Sum(x => (x.Price ?? 0) * x.Quantity)
                 })
                 .ToList();


            return View(grouped);
        }


        // GET: /Admin/Search?query=abc
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var users = await dbContext.Users
                .Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query) || u.Email.Contains(query))
                .Select(u => new {
                    id = u.UserId,
                    adid = u.EmployeeId,   // or your ADID field
                    firstName = u.FirstName,
                    lastName = u.LastName,
                    email = u.Email,
                    role = u.UserRole // Or use IdentityUserRole if you're using Identity
                })
                .ToListAsync();

            return Json(users);
        }

        // POST: /Admin/UpdateRole/5
        [HttpPost]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] RoleUpdateRequest request)
        {
            var user = await dbContext.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.UserRole = request.Role; // "EMPLOYEE" or "ADMIN"
            await dbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        public class RoleUpdateRequest
        {
            public string Role { get; set; }
        }

        [HttpGet]
        public IActionResult ManageReservedOrders(DateTime? startDate, DateTime? endDate)
        {
            // Default to current month if no date filter provided
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var now = DateTime.Now;
                startDate = new DateTime(now.Year, now.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            // Base query: get all orders within date range
            var query = dbContext.OrderSummaryView.AsQueryable();

            query = query.Where(o =>
                o.ReservationDate >= startDate.Value &&
                o.ReservationDate <= endDate.Value &&
                o.Status == "Pending" // ✅ Only pending orders
            );

            // Map to ViewModel
            var model = new ReservedOrderViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                OrderSummaries = query.Select(o => new ReservationViewModel
                {
                    EmployeeId = o.EmployeeId,
                    FirstName = o.FirstName,
                    LastName = o.LastName,
                    Section = o.Section,
                    ReservationDate = o.ReservationDate.ToLocalTime().Date,
                    Time = o.MealTime,
                    MenuType = o.MenuType,
                    Quantity = o.Quantity,
                    ReferenceNumber = o.ReferenceNumber,
                    OrderName = string.IsNullOrWhiteSpace(o.MenuName) ? "No specific menu uploaded yet" : o.MenuName,
                    Status = o.Status
                })
                .OrderBy(o => o.ReservationDate)
                .ToList()
            };

            return View(model); // Razor view: ManageReservedOrders.cshtml
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

        [HttpPost]
        public IActionResult UpdateQuantity(string ReferenceNumber, int Quantity, string OrderName, string MenuType, DateTime? ReservationDate)
        {
            var source = dbContext.CombineOrders
                .Where(x => x.ReferenceNumber == ReferenceNumber)
                .Select(x => x.Source)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(source))
            {
                TempData["UpdateStatus"] = "error";
                TempData["UpdateMessage"] = "Order not found.";
                return RedirectToAction("ManageReservedOrders");
            }

            // Normalize
            MenuType = MenuType?.Trim();
            OrderName = OrderName?.Trim();

            // Step 1: Look for a menu with exact date and menu type
            string menuFromDb = null;

            if (ReservationDate.HasValue && !string.IsNullOrEmpty(MenuType))
            {
                var resDate = ReservationDate.Value.Date;
                var nextDay = resDate.AddDays(1);

                menuFromDb = dbContext.Menus
                    .Where(m =>
                        m.MenuType == MenuType &&
                        m.AvailabilityDate >= resDate &&
                        m.AvailabilityDate < nextDay)
                    .Select(m => m.Name)
                    .FirstOrDefault();
            }



            // Step 2: Force "N/A" if none found or frontend sent placeholder
            if (string.IsNullOrWhiteSpace(menuFromDb) ||
                string.Equals(OrderName, "No menu available", StringComparison.OrdinalIgnoreCase))
            {
                OrderName = "No specific menu uploaded yet";
            }
            else
            {
                OrderName = menuFromDb;
            }

            // Step 3: Update source table
            if (source == "Order")
            {
                var order = dbContext.Orders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);
                if (order != null)
                {
                    order.MenuType = MenuType ?? order.MenuType;
                    order.OrderName = OrderName;
                    order.Quantity = Quantity;
                }
            }
            else if (source == "AdvanceOrder")
            {
                var advOrder = dbContext.AdvanceOrders.FirstOrDefault(x => x.ReferenceNumber == ReferenceNumber);
                if (advOrder != null)
                {
                    advOrder.MenuType = MenuType ?? advOrder.MenuType;
                    advOrder.MenuName = OrderName;
                    advOrder.Quantity = Quantity;
                }
            }

            dbContext.SaveChanges();

            TempData["UpdateStatus"] = "success";
            TempData["UpdateMessage"] = $"Order updated successfully. Menu set to: {MenuType}, Name: {OrderName}, Quantity: {Quantity} ";
            return RedirectToAction("ManageReservedOrders");
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(string ReferenceNumber)
        {
            var source = dbContext.CombineOrders
                .Where(x => x.ReferenceNumber == ReferenceNumber)
                .Select(x => x.Source)
                .FirstOrDefault();

            //if (string.IsNullOrEmpty(source))
            //{
            //    TempData["UpdateStatus"] = "error";
            //    TempData["UpdateMessage"] = "Order not found.";
            //    return RedirectToAction("ManageReservedOrders");
            //}

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

            //TempData["UpdateStatus"] = "success";
            //TempData["UpdateMessage"] = "Order cancelled successfully.";
            //return RedirectToAction("ManageReservedOrders");

            return Json(new { success = true, referenceNumber = ReferenceNumber });
        }


        [HttpGet]
        public IActionResult FilterReservedOrders(string status, DateTime? startDate, DateTime? endDate)
        {
            // Default to current month if no date filter provided
            if (!startDate.HasValue || !endDate.HasValue)
            {
                var now = DateTime.Now;
                startDate = new DateTime(now.Year, now.Month, 1);
                endDate = startDate.Value.AddMonths(1).AddDays(-1);
            }

            // Base query: get all orders within date range
            var query = dbContext.OrderSummaryView.AsQueryable();

            query = query.Where(o =>
                o.ReservationDate >= startDate.Value &&
                o.ReservationDate <= endDate.Value
            );

            // Filter by status only if a value is provided
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            // Map to ViewModel
            var model = new ReservedOrderViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                SelectedStatus = status,
                OrderSummaries = query.Select(o => new ReservationViewModel
                {
                    EmployeeId = o.EmployeeId,
                    FirstName = o.FirstName,
                    LastName = o.LastName,
                    Section = o.Section,
                    ReservationDate = o.ReservationDate.ToLocalTime().Date,
                    Time = o.MealTime,
                    MenuType = o.MenuType,
                    Quantity = o.Quantity,
                    ReferenceNumber = o.ReferenceNumber,
                    OrderName = string.IsNullOrWhiteSpace(o.MenuName) ? "No specific menu uploaded yet" : o.MenuName,
                    Status = o.Status
                })
                .OrderBy(o => o.ReservationDate)
                .ToList()
            };

            return View("ManageReservedOrders", model); // reuse the same Razor view
        }


    }
}
