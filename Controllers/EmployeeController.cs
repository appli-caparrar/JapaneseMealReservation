using JapaneseMealReservation.AppData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JapaneseMealReservation.Controllers
{
    [Route("Employee")]
    public class EmployeeController : Controller
    {
        private readonly SqlServerDbContext sqlServerDbContext;

        public EmployeeController(SqlServerDbContext sqlServerDbContext)
        {
            this.sqlServerDbContext = sqlServerDbContext;
        }

        [AllowAnonymous]
        [HttpGet("GetEmployeeById")]
        public IActionResult GetEmployeeById(string id)
        {
            try
            {
                var user = sqlServerDbContext.View_UserInfo
                .FirstOrDefault(u => u.EmpNo != null && u.EmpNo.Trim().ToUpper() == id.Trim().ToUpper());

                if (user == null)
                    return NotFound();

                return Json(new
                {
                    firstName = user.First_Name,
                    lastName = user.Last_Name,
                    section = user.Section,
                    email = user.Email,
                    position = user.Position,
                    adid = user.ADID
                });
            }
            catch (Exception ex)
            {
                // Log the exception (use your logging mechanism here)
                // For example: _logger.LogError(ex, "Error in GetEmployeeById");

                if (ex.InnerException != null)
                {
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }

                return StatusCode(500, "Internal Server Error: " + ex.Message);
            }
        }
    }
}
