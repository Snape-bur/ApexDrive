using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApexDrive.Data;
using ApexDrive.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class CarsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly string _imageFolderPath = Path.Combine("wwwroot", "images", "cars");

        public CarsController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ INDEX – List Cars (filtered by branch)
        public async Task<IActionResult> Index(string search, int? branchId)
        {
            var query = _context.Cars
                .Include(c => c.Branch)
                .AsQueryable();

            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.BranchId != null)
                    query = query.Where(c => c.BranchId == user.BranchId);
            }

            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.PlateNumber.ToLower().Contains(search) ||
                    c.Brand.ToLower().Contains(search) ||
                    c.Model.ToLower().Contains(search));
            }

            if (branchId.HasValue && branchId.Value > 0)
                query = query.Where(c => c.BranchId == branchId);

            var cars = await query.OrderBy(c => c.Brand).ToListAsync();
            return View(cars);
        }

        // ✅ CREATE (GET)
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            return View();
        }

        // ✅ CREATE (POST) – with Image Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car, IFormFile imageFile)
        {
            // Ensure directory exists
            if (!Directory.Exists(_imageFolderPath))
                Directory.CreateDirectory(_imageFolderPath);

            // Assign branch based on role
            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                car.BranchId = user?.BranchId ?? 0;
            }
            else if (User.IsInRole("SuperAdmin"))
            {
                if (car.BranchId == 0)
                    ModelState.AddModelError("BranchId", "Please select a branch.");
            }

            // Handle image upload
            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(_imageFolderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                // Save relative path
                car.ImageUrl = "/images/cars/" + fileName;
            }

            if (ModelState.IsValid)
            {
                _context.Add(car);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Car added successfully.";
                return RedirectToAction(nameof(Index));
            }

            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            return View(car);
        }



        // ✅ EDIT (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var car = await _context.Cars.Include(c => c.Branch).FirstOrDefaultAsync(c => c.CarId == id);
            if (car == null)
                return NotFound();

            // For Admin, restrict to their own branch
            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (car.BranchId != user?.BranchId)
                    return Forbid();
            }

            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            return View(car);
        }

        // ✅ EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Car formCar, IFormFile? newImageFile)
        {
            Console.WriteLine($"🧠 ENTERING POST Edit for CarId={id}");

            if (id != formCar.CarId)
                return BadRequest();

            var existingCar = await _context.Cars.FindAsync(id);
            if (existingCar == null)
                return NotFound();

            // Update editable fields
            existingCar.PlateNumber = formCar.PlateNumber;
            existingCar.Brand = formCar.Brand;
            existingCar.Model = formCar.Model;
            existingCar.DailyRate = formCar.DailyRate;
            existingCar.Mileage = formCar.Mileage;
            existingCar.Type = formCar.Type;
            existingCar.FuelType = formCar.FuelType;
            existingCar.Transmission = formCar.Transmission;
            existingCar.BranchId = formCar.BranchId;

            // ✅ Handle image replacement only if a new one is uploaded
            if (newImageFile != null && newImageFile.Length > 0)
            {
                if (!Directory.Exists(_imageFolderPath))
                    Directory.CreateDirectory(_imageFolderPath);

                // Delete old image if exists
                if (!string.IsNullOrEmpty(existingCar.ImageUrl))
                {
                    var oldPath = Path.Combine("wwwroot", existingCar.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newImageFile.FileName);
                var filePath = Path.Combine(_imageFolderPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await newImageFile.CopyToAsync(stream);

                existingCar.ImageUrl = "/images/cars/" + fileName;
                TempData["Success"] = "✅ Image updated successfully.";
            }

            // ✅ Keep existing image if no new one uploaded
            if (newImageFile == null || newImageFile.Length == 0)
                Console.WriteLine("ℹ️ Keeping existing image (no upload detected).");

            // ✅ Save
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(existingCar);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "✅ Car updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error while saving: {ex.Message}");
                    TempData["Error"] = "⚠️ Could not save changes. Try again.";
                }
            }

            // Refill branch list if SuperAdmin
            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            return View(formCar);
        }

        // ✅ DELETE (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var car = await _context.Cars
                .Include(c => c.Branch)
                .FirstOrDefaultAsync(c => c.CarId == id);

            if (car == null) return NotFound();
            return View(car);
        }

        // ✅ DELETE (POST)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car != null)
            {
                // Delete image file if exists
                if (!string.IsNullOrEmpty(car.ImageUrl))
                {
                    var filePath = Path.Combine("wwwroot", car.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                _context.Cars.Remove(car);
                await _context.SaveChangesAsync();
                TempData["Success"] = "🗑️ Car deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ TOGGLE AVAILABILITY
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
                return NotFound();

            // 🔒 Restrict Admin to their own branch
            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (car.BranchId != user?.BranchId)
                    return Forbid(); // Prevent cross-branch toggle
            }

            // ✅ Toggle availability
            car.IsAvailable = !car.IsAvailable;
            await _context.SaveChangesAsync();

            TempData["Success"] = car.IsAvailable
                ? $"🚘 {car.PlateNumber} is now available."
                : $"🚫 {car.PlateNumber} marked unavailable.";

            return RedirectToAction(nameof(Index));
        }

    }
}
