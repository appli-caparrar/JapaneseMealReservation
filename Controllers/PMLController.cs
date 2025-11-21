using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace JapaneseMealReservation.Controllers
{
    public class PMLController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> SendDeveloperMessage(IFormFile screenshot, string subject, string description)
        {
            var section = User.FindFirst("Section")?.Value ?? "Unknown";
            var pic = User.Identity?.Name ?? "Unknown";

            var excelPath = @"\\apbiphsh04\C0_Project\10_system_development\00_IT Hashira Documents\FY2025\Japanese Meal Reservation System\05_PML,AML & Activity Plan\Japanese Meal_Problem Management List.xlsm";

            if (!System.IO.File.Exists(excelPath))
                return Json(new { success = false, message = "Excel file not found." });

            try
            {
                using (var workbook = new XLWorkbook(excelPath))
                {
                    var ws = workbook.Worksheet("Masterlist");
                    if (ws == null)
                        return Json(new { success = false, message = "Masterlist sheet not found." });

                    var newRow = ws.LastRowUsed()?.RowNumber() + 1 ?? 2;

                    ws.Cell(newRow, 1).Value = DateTime.Now.ToString("dd-MMM-yy");
                    ws.Cell(newRow, 2).Value = section;
                    ws.Cell(newRow, 3).Value = pic;
                    ws.Cell(newRow, 4).Value = subject;
                    ws.Cell(newRow, 5).Value = description;

                    if (screenshot != null && screenshot.Length > 0)
                    {
                        var uploads = Path.Combine("wwwroot", "screenshots");
                        if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

                        var fileName = $"{Guid.NewGuid()}_{screenshot.FileName}";
                        var filePath = Path.Combine(uploads, fileName);

                        await using (var fs = new FileStream(filePath, FileMode.Create))
                        {
                            await screenshot.CopyToAsync(fs);
                        }

                        ws.Cell(newRow, 6).Value = $"/screenshots/{fileName}";
                    }

                    workbook.Save(); //Saves directly to the same file
                }

                return Json(new { success = true, message = "Message logged to Excel!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

    }
}
