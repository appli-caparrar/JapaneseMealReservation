using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using DocumentFormat.OpenXml.InkML;
using JapaneseMealReservation.AppData;
using JapaneseMealReservation.Models;
using JapaneseMealReservation.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace JapaneseMealReservation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext dbContext;
        private readonly SqlServerDbContext sqlDbContext;

        public HomeController(ILogger<HomeController> logger, AppDbContext dbContext, SqlServerDbContext sqlDbContext)
        {
            _logger = logger;
            this.dbContext = dbContext;
            this.sqlDbContext = sqlDbContext;
        }
     

        public IActionResult Index()
        {
            return View();
        }

       

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(Login model)
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
                return View(model);
            }

            // Create a list of claims (user identity data), here storing the user's first name
            var claims = new List<Claim>
            {
                new Claim("EmployeeId", user.EmployeeId ?? string.Empty),  // custom claim type string
                new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
                new Claim(ClaimTypes.Surname, user.LastName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim("Section", user.Section ?? ""),  // custom claim type string
                new Claim(ClaimTypes.Role, user.UserRole ?? ""),
                new Claim("EmployeeType", user.EmployeeType ?? "")
            };

            // Create a ClaimsIdentity using the claims and specify the authentication scheme
            var identity = new ClaimsIdentity(claims, "MyCookieAuth");

            // Create a ClaimsPrincipal that holds the identity
            var principal = new ClaimsPrincipal(identity);

            // Sign in the user by issuing the authentication cookie
            await HttpContext.SignInAsync("MyCookieAuth", principal);

            // Redirect the authenticated user to the welcome page
            return RedirectToAction("Index", "Home");
        }

        //[HttpGet]
        //[AllowAnonymous]
        //public IActionResult IportalConfirmationForm(string? ip = null)
        //{
        //    // Use the passed IP or fallback to server-side IP
        //    string userIP = GetClientIp(HttpContext);

        //    return View(model: userIP);
        //}

        //public string GetClientIp(HttpContext context)
        //{
        //    var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        //    if (!string.IsNullOrWhiteSpace(forwardedHeader))
        //    {
        //        return forwardedHeader.Split(',')[0].Trim();
        //    }

        //    var remoteIp = context.Connection.RemoteIpAddress;

        //    if (remoteIp != null)
        //    {
        //        if (remoteIp.IsIPv4MappedToIPv6)
        //        {
        //            return remoteIp.MapToIPv4().ToString(); // Converts ::ffff:127.0.0.1 to 127.0.0.1
        //        }

        //        // Convert ::1 to 127.0.0.1 manually
        //        if (remoteIp.ToString() == "::1")
        //        {
        //            return remoteIp.ToString();
        //        }

        //        return remoteIp.ToString();
        //    }

        //    return "IP Not Found";
        //}


        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> Login()
        //{
        //    string localIP = GetClientIp(HttpContext); // use helper method
        //    if (IPAddress.TryParse(localIP, out var ip))
        //    {
        //        if (ip.IsIPv4MappedToIPv6)
        //            localIP = ip.MapToIPv4().ToString(); // Force to IPv4 format
        //    }

        //    //Console.WriteLine($"Raw Remote IP: {HttpContext.Connection.RemoteIpAddress}");
        //    //Console.WriteLine($"Client IP: {GetClientIp(HttpContext)}");

        //    long systemId = 70; // Change if needed

        //    //Console.WriteLine($"Local IP: {localIP}");

        //    // ✅ Get the latest login request for the IP
        //    var loginEntry = await sqlDbContext.Tbl_LOGIN_Request
        //        .Where(x => x.IpAddress == localIP && x.SystemId == systemId)
        //        .OrderByDescending(x => x.Id)
        //        .FirstOrDefaultAsync();

        //    // Condition 1: No login request OR
        //    // Condition 2: Request found but not active or missing EmployeeId
        //    if (loginEntry == null ||
        //        string.IsNullOrWhiteSpace(loginEntry.EmployeeId) ||
        //        !loginEntry.Status.Equals("ACTIVE", StringComparison.OrdinalIgnoreCase))
        //    {
        //        // Show the IportalConfirmationForm if not using WinForms app
        //        //return RedirectToAction("IportalConfirmationForm", "Home");
        //        return View();
        //    }

        //    // Lookup the Employee in your main Users table
        //    var user = await dbContext.Users
        //        .FirstOrDefaultAsync(x => x.EmployeeId == loginEntry.EmployeeId);

        //    if (user == null)
        //    {
        //        TempData["ShowRegisterAlert"] = true;
        //        return RedirectToAction("Register", "Home");
        //    }

        //    // Sign in logic
        //    var claims = new List<Claim>
        //    {
        //        new Claim("EmployeeId", user.EmployeeId ?? ""),
        //        new Claim(ClaimTypes.GivenName, user.FirstName ?? ""),
        //        new Claim(ClaimTypes.Surname, user.LastName ?? ""),
        //        new Claim(ClaimTypes.Email, user.Email ?? ""),
        //        new Claim("Section", user.Section ?? ""),
        //        new Claim(ClaimTypes.Role, user.UserRole ?? ""),
        //        new Claim("EmployeeType", user.EmployeeType ?? "")
        //    };

        //    Console.WriteLine("Redirecting to IportalConfirmationForm due to: " +
        //    (loginEntry == null ? "No login entry" :
        //    string.IsNullOrWhiteSpace(loginEntry.EmployeeId) ? "Missing EmployeeId" :
        //    "Inactive status"));

        //    var identity = new ClaimsIdentity(claims, "MyCookieAuth");
        //    var principal = new ClaimsPrincipal(identity);
        //    await HttpContext.SignInAsync("MyCookieAuth", principal);

        //    return RedirectToAction("Index", "Home");
        //}

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login", "Home");
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        //[AllowAnonymous]
        //[HttpPost]
        //public IActionResult Register(User model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }

        //    //Set role
        //    model.UserRole = model.Section?.ToUpper() == "GA" ? "ADMIN" : "EMPLOYEE";

        //    // Set employee type if ID contains "BIPH-JP"
        //    if (!string.IsNullOrEmpty(model.EmployeeId) && model.EmployeeId.Contains("BIPH-JP", StringComparison.OrdinalIgnoreCase))
        //    {
        //        model.EmployeeType = "Expat";
        //    }
        //    else
        //    {
        //        model.EmployeeType = "Local"; 
        //    }

        //    dbContext.Users.Add(model);
        //    dbContext.SaveChanges();

        //    TempData["RegisterSuccess"] = true;

        //    return RedirectToAction("Register");
        //}

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Register(User model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Determine role and employee type (used in PostgreSQL save)
            model.UserRole = model.Section?.ToUpper() == "GA" ? "ADMIN" : "EMPLOYEE";
            model.EmployeeType = model.EmployeeId?.Contains("BIPH-JP", StringComparison.OrdinalIgnoreCase) == true
                ? "Expat"
                : "Local";

            // Exclude Position and ADID from PostgreSQL insert by creating a stripped model
            var pgUser = new User
            {
                EmployeeId = model.EmployeeId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Section = model.Section,
                Password = model.Password,
                CreatedDate = DateTime.UtcNow,
                UserRole = model.UserRole,
                EmployeeType = model.EmployeeType
                // Do not assign Position and ADID here – they are not in the PostgreSQL 'users' table
            };

            // Save to PostgreSQL
            dbContext.Users.Add(pgUser);
            dbContext.SaveChanges();

            // Save approver data to SQL Server (these fields may exist in SQL Server only)
            var newApprover = new CasSystemApproverList
            {
                SystemID = "70",
                SystemName = "Japanese Meal Reservation System",
                ApproverNumber = "0",
                FullName = $"{model.FirstName} {model.LastName}",
                EmailAddress = model.Email,
                SECTION = model.Section,
                POSITION = model.Position,
                ADID = model.ADID,
                EmployeeNumber = model.EmployeeId
            };

            sqlDbContext.Tbl_System_Approver_list.Add(newApprover);
            sqlDbContext.SaveChanges();

            TempData["RegisterSuccess"] = true;
            return RedirectToAction("Register");
        }



        //[AllowAnonymous]
        [HttpGet]
        public IActionResult GetEmployeeById(string id)
        {
            var user = dbContext.Users.FirstOrDefault(u => u.EmployeeId == id);

            if (user == null)
                return NotFound();

            return Json(new
            {
                firstName = user.FirstName,
                lastName = user.LastName,
                section = user.Section,
                email = user.Email
            });
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = dbContext.Users.FirstOrDefault(u => u.EmployeeId == model.EmployeeId);

            if (user == null)
            {
                // Associate error with EmployeeId field
                ModelState.AddModelError(nameof(model.EmployeeId), "Employee ID not found.");
                return View(model);
            }

            // Ideally: Hash the password before storing
            user.Password = model.Password;

            dbContext.SaveChanges();

            TempData["Message"] = "Password successfully updated.";
            return RedirectToAction("Login");
        }


        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
