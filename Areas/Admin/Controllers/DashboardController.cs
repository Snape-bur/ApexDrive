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

        // 🏠 Dashboard Index (Cards + Tables)
        public async Task<IActionResult> Index(DateTime? selectedDate, DateTime? fromDate, DateTime? toDate)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            int? branchId = null;
            if (!isSuperAdmin)
            {
                branchId = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();
            }

            // ───────────────────────────────
            // Base Queries
            // ───────────────────────────────
            var carsQuery = _context.Cars.AsQueryable();
            var bookingsQuery = _context.Bookings.AsQueryable();
            var remindersQuery = _context.CarReminders
                .Include(r => r.Car)
                .Include(r => r.Branch)
                .AsQueryable();
            var maintenanceQuery = _context.CarMaintenanceHistories.AsQueryable();
            var paymentsQuery = _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Car)
                .AsQueryable();

           
            // 📅 Revenue Filter (Range > Single Day)
            if (fromDate.HasValue && toDate.HasValue)
            {
                var start = fromDate.Value.Date;
                var end = toDate.Value.Date.AddDays(1).AddTicks(-1);

                paymentsQuery = paymentsQuery.Where(p =>
                    p.CreatedAt >= start && p.CreatedAt <= end);
            }
            else if (selectedDate.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p =>
                    p.CreatedAt.Date == selectedDate.Value.Date);
            }


            // 🏢 Branch Restriction (Admin only)
            if (branchId.HasValue)
            {
                carsQuery = carsQuery.Where(c => c.BranchId == branchId);
                bookingsQuery = bookingsQuery.Where(b =>
                    b.PickupBranchId == branchId || b.Car.BranchId == branchId);
                remindersQuery = remindersQuery.Where(r => r.BranchId == branchId);
                maintenanceQuery = maintenanceQuery.Where(m => m.BranchId == branchId);
                paymentsQuery = paymentsQuery.Where(p =>
                    p.Booking.Car.BranchId == branchId ||
                    p.Booking.PickupBranchId == branchId);
            }

            // ───────────────────────────────
            // ViewModel
            // ───────────────────────────────
            var model = new DashboardViewModel
            {
                BranchName = branchId.HasValue
                    ? await _context.Branches
                        .Where(b => b.BranchId == branchId)
                        .Select(b => b.BranchName)
                        .FirstOrDefaultAsync()
                    : "All Branches",

                SelectedDate = selectedDate,

                FromDate = fromDate,
                ToDate = toDate,

                TotalCars = await carsQuery.CountAsync(),
                ActiveBookings = await bookingsQuery.CountAsync(),

                UpcomingReminders = await remindersQuery
                    .Where(r => !r.IsCompleted && r.ReminderDate <= DateTime.UtcNow.AddDays(7))
                    .CountAsync(),

                CompletedMaintenances = await maintenanceQuery.CountAsync(),

                TotalRevenue = await paymentsQuery
                    .SumAsync(p => (decimal?)p.Amount) ?? 0
            };

            // ───────────────────────────────
            // 🔔 Reminder Summary (UNCHANGED)
            // ───────────────────────────────
            var today = DateTime.UtcNow.Date;
            var activeReminders = remindersQuery.Where(r => !r.IsCompleted);

            model.ReminderSummary = new ReminderSummary
            {
                OverdueCount = await activeReminders.CountAsync(r => r.ReminderDate < today),
                DueTodayCount = await activeReminders.CountAsync(r => r.ReminderDate == today),
                UpcomingCount = await activeReminders.CountAsync(r =>
                    r.ReminderDate > today && r.ReminderDate <= today.AddDays(3)),
                UpcomingReminders = await activeReminders
                    .Where(r => r.ReminderDate >= today && r.ReminderDate <= today.AddDays(3))
                    .OrderBy(r => r.ReminderDate)
                    .Take(3)
                    .ToListAsync()
            };

            // ───────────────────────────────
            // 💰 Super Admin – Revenue Per Branch (NEW)
            // ───────────────────────────────
            if (isSuperAdmin)
            {
                model.RevenuePerBranch = await paymentsQuery
                    .GroupBy(p => p.Booking.Car.BranchId)
                    .Select(g => new RevenuePerBranchVM
                    {
                        BranchId = g.Key,
                        BranchName = _context.Branches
                            .Where(b => b.BranchId == g.Key)
                            .Select(b => b.BranchName)
                            .FirstOrDefault(),
                        Revenue = g.Sum(x => x.Amount)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .ToListAsync();
            }

            return View(model);
        }

        // 📈 API for Charts (UNCHANGED, SAFE)
        [HttpGet]
        public async Task<IActionResult> GetDashboardData(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= DateTime.UtcNow.AddMonths(-6);
            toDate ??= DateTime.UtcNow;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            int? branchId = null;
            if (!isSuperAdmin)
            {
                branchId = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.BranchId)
                    .FirstOrDefaultAsync();
            }

            var bookingsQuery = _context.Bookings
                .Where(b => b.StartDate >= fromDate && b.StartDate <= toDate);

            var maintenanceQuery = _context.CarMaintenanceHistories
                .Where(m => m.ServiceDate >= fromDate && m.ServiceDate <= toDate);

            var paymentsQuery = _context.Payments
                .Where(p => p.CreatedAt >= fromDate && p.CreatedAt <= toDate);

            if (branchId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.PickupBranchId == branchId || b.Car.BranchId == branchId);
                maintenanceQuery = maintenanceQuery.Where(m => m.BranchId == branchId);
                paymentsQuery = paymentsQuery.Where(p =>
                    p.Booking.Car.BranchId == branchId ||
                    p.Booking.PickupBranchId == branchId);
            }

            var monthlyBookings = await bookingsQuery
                .GroupBy(b => new { b.StartDate.Year, b.StartDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Month = $"{g.Key.Month:D2}-{g.Key.Year}",
                    Count = g.Count()
                })
                .ToListAsync();

            var maintenanceStats = await maintenanceQuery
                .GroupBy(m => new { m.ServiceDate.Year, m.ServiceDate.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Month = $"{g.Key.Month:D2}-{g.Key.Year}",
                    Count = g.Count()
                })
                .ToListAsync();

            var carsPerBranch = isSuperAdmin
                ? await _context.Branches
                    .Select(b => new
                    {
                        b.BranchName,
                        CarCount = b.Cars.Count()
                    })
                    .ToListAsync()
                : null;

            var revenuePerBranch = isSuperAdmin
                ? await paymentsQuery
                    .GroupBy(p => p.Booking.Car.BranchId)
                    .Select(g => new
                    {
                        BranchId = g.Key,
                        Revenue = g.Sum(x => x.Amount)
                    })
                    .Join(
                        _context.Branches,
                        r => r.BranchId,
                        b => b.BranchId,
                        (r, b) => new
                        {
                            BranchName = b.BranchName,
                            Revenue = r.Revenue
                        })
                    .ToListAsync()
                : null;

            var totalRevenue = await paymentsQuery
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return Json(new
            {
                totalRevenue,
                monthlyBookings,
                maintenanceStats,
                carsPerBranch,
                revenuePerBranch
            });
        }
    }
}
