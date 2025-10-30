using ApexDrive.Data;
using ApexDrive.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class CarsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

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

            // 🔍 Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(c =>
                    c.PlateNumber.ToLower().Contains(search) ||
                    c.Brand.ToLower().Contains(search) ||
                    c.Model.ToLower().Contains(search));
            }

            // 🏢 Filter by branch (SuperAdmin only)
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
        // ✅ CREATE (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car)
        {
            // Determine branch based on role
            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                car.BranchId = user?.BranchId ?? 0;

                // Debugging admin branch assignment
                Console.WriteLine($"[DEBUG] Admin creating car → BranchId set to {car.BranchId}");
            }
            else if (User.IsInRole("SuperAdmin"))
            {
                if (car.BranchId == 0)
                    ModelState.AddModelError("BranchId", "Please select a branch.");
            }

            // Validate state before saving
            if (ModelState.IsValid)
            {
                _context.Add(car);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Car added successfully.";
                Console.WriteLine("✅ Car successfully added.");
                return RedirectToAction(nameof(Index));
            }

            // Debug ModelState errors
            Console.WriteLine("❌ ModelState Invalid — Debug Output:");
            foreach (var entry in ModelState)
            {
                var key = entry.Key;
                var errors = string.Join(", ", entry.Value.Errors.Select(e => e.ErrorMessage));
                if (!string.IsNullOrEmpty(errors))
                    Console.WriteLine($"   • {key}: {errors}");
            }

            // Repopulate dropdown if validation fails
            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            return View(car);
        }


        // ✅ EDIT (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

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
        public async Task<IActionResult> Edit(int id, Car car)
        {
            if (id != car.CarId) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(car);
                await _context.SaveChangesAsync();
                TempData["Success"] = "✅ Car updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            Console.WriteLine("❌ ModelState Invalid on Edit:");
            foreach (var entry in ModelState)
            {
                var key = entry.Key;
                var errors = string.Join(", ", entry.Value.Errors.Select(e => e.ErrorMessage));
                if (!string.IsNullOrEmpty(errors))
                    Console.WriteLine($"   • {key}: {errors}");
            }

            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            return View(car);
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
                _context.Cars.Remove(car);
                await _context.SaveChangesAsync();
                TempData["Success"] = "🗑️ Car deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ Toggle Availability
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null)
                return NotFound();

            car.IsAvailable = !car.IsAvailable;
            await _context.SaveChangesAsync();

            TempData["Success"] = car.IsAvailable
                ? $"🚘 {car.PlateNumber} is now available."
                : $"🚫 {car.PlateNumber} marked unavailable.";

            return RedirectToAction(nameof(Index));
        }
    }
}
