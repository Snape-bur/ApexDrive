using ApexDrive.Data;
using ApexDrive.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🏠 Dashboard Index
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Admin";
            int? branchId = null;

            if (userRole == "Admin")
            {
                branchId = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();
            }

            // 🧮 Query Setup
            var carsQuery = _context.Cars.AsQueryable();
            var bookingsQuery = _context.Bookings.AsQueryable();
            var remindersQuery = _context.CarReminders.AsQueryable();
            var maintenanceQuery = _context.CarMaintenanceHistories.AsQueryable();

            if (branchId.HasValue)
            {
                carsQuery = carsQuery.Where(c => c.BranchId == branchId);
                bookingsQuery = bookingsQuery
                    .Where(b => b.PickupBranchId == branchId || b.Car.BranchId == branchId);
                remindersQuery = remindersQuery.Where(r => r.BranchId == branchId);
                maintenanceQuery = maintenanceQuery.Where(m => m.BranchId == branchId);
            }

            // 📊 Build ViewModel
            var model = new DashboardViewModel
            {
                BranchName = branchId.HasValue
                    ? await _context.Branches
                        .Where(b => b.BranchId == branchId)
                        .Select(b => b.BranchName)
                        .FirstOrDefaultAsync()
                    : "All Branches",

                TotalCars = await carsQuery.CountAsync(),
                ActiveBookings = await bookingsQuery.CountAsync(), // refine later if needed
                UpcomingReminders = await remindersQuery
                    .Where(r => !r.IsCompleted && r.ReminderDate <= DateTime.Today.AddDays(7))
                    .CountAsync(),
                CompletedMaintenances = await maintenanceQuery.CountAsync(),
                TotalRevenue = await _context.Payments.SumAsync(p => (decimal?)p.Amount) ?? 0,
                RecentMaintenanceRecords = await maintenanceQuery
                    .OrderByDescending(m => m.ServiceDate)
                    .Take(5)
                    .Include(m => m.Car)
                    .ToListAsync()
            };

            return View(model);
        }

        // 📈 API Endpoint for Chart.js
        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = User.IsInRole("SuperAdmin") ? "SuperAdmin" : "Admin";
            int? branchId = null;

            if (userRole == "Admin")
            {
                branchId = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();
            }

            var bookingsQuery = _context.Bookings.AsQueryable();
            var maintenanceQuery = _context.CarMaintenanceHistories.AsQueryable();

            if (branchId.HasValue)
            {
                bookingsQuery = bookingsQuery
                    .Where(b => b.PickupBranchId == branchId || b.Car.BranchId == branchId);
                maintenanceQuery = maintenanceQuery.Where(m => m.BranchId == branchId);
            }

            // 🗓 Monthly Bookings (Chronological)
            var monthlyBookingsRaw = await bookingsQuery
      .GroupBy(b => new { b.StartDate.Year, b.StartDate.Month })
      .Select(g => new
      {
          g.Key.Year,
          g.Key.Month,
          Count = g.Count()
      })
      .OrderBy(g => g.Year)
      .ThenBy(g => g.Month)
      .ToListAsync();

            var monthlyBookings = monthlyBookingsRaw
                .Select(g => new
                {
                    Month = $"{g.Month:D2}-{g.Year}",
                    g.Count
                })
                .ToList();


            // 🧰 Maintenance Frequency (Chronological)
            var maintenanceRaw = await maintenanceQuery
      .GroupBy(m => new { m.ServiceDate.Year, m.ServiceDate.Month })
      .Select(g => new
      {
          g.Key.Year,
          g.Key.Month,
          Count = g.Count()
      })
      .OrderBy(g => g.Year)
      .ThenBy(g => g.Month)
      .ToListAsync();

            var maintenanceStats = maintenanceRaw
                .Select(g => new
                {
                    Month = $"{g.Month:D2}-{g.Year}",
                    g.Count
                })
                .ToList();

            // 🚗 Cars per Branch (SuperAdmin only)
            var carsPerBranch = userRole == "SuperAdmin"
                ? await _context.Branches
                    .Select(b => new
                    {
                        b.BranchName,
                        CarCount = b.Cars.Count()
                    })
                    .OrderBy(b => b.BranchName)
                    .ToListAsync()
                : null;

            // ✅ Return JSON for Chart.js
            return Json(new
            {
                monthlyBookings,
                maintenanceStats,
                carsPerBranch
            });
        }
    }
}
