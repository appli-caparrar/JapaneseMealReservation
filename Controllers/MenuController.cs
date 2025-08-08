using ClosedXML.Excel;
using JapaneseMealReservation.AppData;
using JapaneseMealReservation.Models;
using JapaneseMealReservation.ViewModels;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;

namespace JapaneseMealReservation.Controllers
{
    [Route("Menu")]
    public class MenuController : Controller
    {
        private readonly AppDbContext dbContext;

        public MenuController(AppDbContext dbContext)
        {
            this.dbContext = dbContext; 
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheets.First();
            var lastRow = worksheet.LastRowUsed();

            if (lastRow == null)
                return BadRequest("The Excel sheet is empty or contains no usable data.");

            var menus = new List<Menu>();

            for (int row = 2; row <= lastRow.RowNumber(); row++)
            {
                // Safely try to read the date
                var dateCell = worksheet.Cell(row, 1);
                if (!dateCell.TryGetValue<DateTime>(out var rawDate))
                    continue; // Skip if not a valid date

                var availabilityDate = DateTime.SpecifyKind(rawDate, DateTimeKind.Utc);

                var name = worksheet.Cell(row, 3).GetString();
                var description = worksheet.Cell(row, 4).GetString();
                var priceText = worksheet.Cell(row, 5).GetString(); // Corrected: price is column 5
                var menuType = worksheet.Cell(row, 6).GetString();

                if (string.IsNullOrWhiteSpace(name)) continue;
                if (!decimal.TryParse(priceText, out var price)) continue;

                menus.Add(new Menu
                {
                    AvailabilityDate = availabilityDate,
                    Name = name,
                    Description = description,
                    Price = price,
                    MenuType = menuType,
                    IsAvailable = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            dbContext.Menus.AddRange(menus);
            await dbContext.SaveChangesAsync();

            TempData["UploadMessage"] = $"{menus.Count} menu item(s) uploaded successfully.";
            return RedirectToAction("Dashboard", "Admin");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMenu(int id, IFormFile? image, string? name, decimal? price, string? description, DateTime? UpdatedAt)
        {
            if (id == 0)
            {
                return BadRequest("Invalid item id.");
            }

            // Create a new Menu object with just the Id (not fully loaded)
            var menu = new Menu { Id = id };

            // Attach to context without loading from DB
            dbContext.Menus.Attach(menu);

            // Update only these properties explicitly

            if (!string.IsNullOrEmpty(name))
            {
                menu.Name = name;
                dbContext.Entry(menu).Property(m => m.Name).IsModified = true;
            }

            if (price.HasValue)
            {
                menu.Price = price.Value;
                dbContext.Entry(menu).Property(m => m.Price).IsModified = true;
            }

            if (!string.IsNullOrEmpty(description))
            {
                menu.Description = description;
                dbContext.Entry(menu).Property(m => m.Description).IsModified = true;
            }


            menu.UpdatedAt = DateTime.UtcNow;
            dbContext.Entry(menu).Property(m => m.UpdatedAt).IsModified = true;
        

            if (image != null && image.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Path.GetFileNameWithoutExtension(image.FileName)}_{id}_{System.Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                menu.ImagePath = "/uploads/" + uniqueFileName;
                dbContext.Entry(menu).Property(m => m.ImagePath).IsModified = true;
            }

            // Save changes to update only these fields
            await dbContext.SaveChangesAsync();

            TempData["UploadMessage"] = $"Menu item updated successfully.";

            return RedirectToAction("Dashboard", "Admin");
        }

        //public IActionResult Dashboard()
        //{
        //    var menus = dbContext.Menus.ToList();
        //    menus = menus.OrderBy(m => m.Id).ToList();  // sort in-memory

        //    return View(menus); // Passes the list to the view
        //}
        public IActionResult Dashboard()
        {
            var model = new DashboardPageModel
            {
                Menus = dbContext.Menus.ToList(),
                Order = new Order() // Optional, can be null
            };

            return View(model); // Must return the full DashboardPageModel
        }

        [HttpPost("Menu/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            var menu = await dbContext.Menus.FindAsync(id);

            if (menu == null)
            {
                return NotFound("Menu item not found.");
            }

            // Optional: Delete image file from server if it exists
            if (!string.IsNullOrEmpty(menu.ImagePath))
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", menu.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            dbContext.Menus.Remove(menu);
            await dbContext.SaveChangesAsync();

            TempData["UploadMessage"] = $"Menu item deleted successfully.";

            return RedirectToAction("Dashboard", "Admin");
        }


    }
}
