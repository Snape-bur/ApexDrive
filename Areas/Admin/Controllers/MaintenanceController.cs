using System;
using System.Linq;
using System.Threading.Tasks;
using ApexDrive.Data;
using ApexDrive.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public MaintenanceController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // INDEX
        public async Task<IActionResult> Index(string search, int? branchId, DateTime? from, DateTime? to)
        {
            var q = _context.CarMaintenanceHistories
                .Include(h => h.Car).ThenInclude(c => c.Branch)
                .Include(h => h.Branch)
                .AsQueryable();

            var me = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin") && me?.BranchId != null)
                q = q.Where(h => h.BranchId == me.BranchId);

            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            if (branchId.HasValue && branchId.Value > 0)
                q = q.Where(h => h.BranchId == branchId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(h =>
                    h.Car.PlateNumber.ToLower().Contains(s) ||
                    h.Car.Brand.ToLower().Contains(s) ||
                    h.Car.Model.ToLower().Contains(s) ||
                    (h.Notes != null && h.Notes.ToLower().Contains(s)) ||
                    (h.ServiceType != null && h.ServiceType.ToLower().Contains(s)));
            }

            if (from.HasValue) q = q.Where(h => h.ServiceDate >= from.Value);
            if (to.HasValue) q = q.Where(h => h.ServiceDate <= to.Value);

            var data = await q.OrderByDescending(h => h.ServiceDate).ToListAsync();
            return View(data);
        }
        public async Task<IActionResult> Create()
        {
            var me = await _userManager.GetUserAsync(User);

            IQueryable<Car> carsQ = _context.Cars.Include(c => c.Branch);
            if (User.IsInRole("Admin") && me?.BranchId != null)
                carsQ = carsQ.Where(c => c.BranchId == me.BranchId);

            var carList = await carsQ
                .Select(c => new { c.CarId, Display = c.PlateNumber + " - " + c.Brand + " " + c.Model })
                .ToListAsync();

            ViewBag.Cars = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(carList, "CarId", "Display");
            return View();
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
      [Bind("CarId,ServiceDate,Mileage,ServiceType,Notes")] CarMaintenanceHistory m)
        {
            Console.WriteLine("?? POST /Admin/Maintenance/Create triggered");

            var car = await _context.Cars.FindAsync(m.CarId);
            if (car == null)
                ModelState.AddModelError(nameof(m.CarId), "Invalid car selected.");

            // Assign BranchId according to role
            if (User.IsInRole("Admin"))
            {
                var me = await _userManager.GetUserAsync(User);
                if (me?.BranchId == null)
                    ModelState.AddModelError(nameof(m.BranchId), "Admin has no assigned branch.");
                m.BranchId = me?.BranchId ?? 0;
                Console.WriteLine($"?? Admin → BranchId = {m.BranchId}");
            }
            else
            {
                m.BranchId = car?.BranchId ?? 0;
                Console.WriteLine($"?? SuperAdmin → BranchId = {m.BranchId}");
            }

            // Don’t validate navigation properties (not posted)
            ModelState.Remove(nameof(CarMaintenanceHistory.Car));
            ModelState.Remove(nameof(CarMaintenanceHistory.Branch));

            if (!ModelState.IsValid)
            {
                // repopulate the dropdown
                var me = await _userManager.GetUserAsync(User);
                IQueryable<Car> carsQ = _context.Cars.Include(c => c.Branch);
                if (User.IsInRole("Admin") && me?.BranchId != null)
                    carsQ = carsQ.Where(c => c.BranchId == me.BranchId);

                var carList = await carsQ
                    .Select(c => new { c.CarId, Display = c.PlateNumber + " - " + c.Brand + " " + c.Model })
                    .ToListAsync();

                ViewBag.Cars = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(carList, "CarId", "Display");
                return View(m);
            }

            m.CreatedAt = DateTime.UtcNow;
            _context.CarMaintenanceHistories.Add(m);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Maintenance record added.";
            return RedirectToAction(nameof(Index));
        }


        // EDIT (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _context.CarMaintenanceHistories
                .Include(x => x.Car)
                .FirstOrDefaultAsync(x => x.HistoryId == id);
            if (m == null) return NotFound();

            if (User.IsInRole("Admin"))
            {
                var me = await _userManager.GetUserAsync(User);
                if (m.BranchId != me?.BranchId) return Forbid();
                ViewBag.Cars = await _context.Cars.Where(c => c.BranchId == me.BranchId).ToListAsync();
            }
            else
            {
                ViewBag.Cars = await _context.Cars.Include(c => c.Branch).ToListAsync();
            }

            return View(m);
        }

        // EDIT (POST)
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CarMaintenanceHistory input)
        {
            if (id != input.HistoryId) return NotFound();
            var m = await _context.CarMaintenanceHistories.FindAsync(id);
            if (m == null) return NotFound();

            var car = await _context.Cars.FindAsync(input.CarId);
            if (car == null) return BadRequest();

            if (User.IsInRole("Admin"))
            {
                var me = await _userManager.GetUserAsync(User);
                if (car.BranchId != me?.BranchId) return Forbid();
                m.BranchId = me!.BranchId!.Value;
            }
            else
            {
                m.BranchId = car.BranchId;
            }

            if (!ModelState.IsValid) return View(input);

            m.CarId = input.CarId;
            m.ServiceDate = input.ServiceDate;
            m.Mileage = input.Mileage;
            m.ServiceType = input.ServiceType;
            m.Notes = input.Notes;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Maintenance updated.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.CarMaintenanceHistories
                .Include(h => h.Car).ThenInclude(c => c.Branch)
                .FirstOrDefaultAsync(h => h.HistoryId == id);
            if (m == null) return NotFound();

            if (User.IsInRole("Admin"))
            {
                var me = await _userManager.GetUserAsync(User);
                if (m.BranchId != me?.BranchId) return Forbid();
            }
            return View(m);
        }

        // DELETE (POST)
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var m = await _context.CarMaintenanceHistories.FindAsync(id);
            if (m != null)
            {
                if (User.IsInRole("Admin"))
                {
                    var me = await _userManager.GetUserAsync(User);
                    if (m.BranchId != me?.BranchId) return Forbid();
                }
                _context.CarMaintenanceHistories.Remove(m);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Maintenance deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
