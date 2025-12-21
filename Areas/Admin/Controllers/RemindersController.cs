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
    public class RemindersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public RemindersController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // INDEX
        public async Task<IActionResult> Index(string search, string type, bool? onlyOverdue, int? branchId, DateTime? from, DateTime? to)
        {
            var q = _context.CarReminders
                .Include(r => r.Car).ThenInclude(c => c.Branch)
                .Include(r => r.Branch)
                .AsQueryable();

            // Admin: restrict to own branch
            var me = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin") && me?.BranchId != null)
                q = q.Where(r => r.BranchId == me.BranchId);

            // SuperAdmin: expose branch filter dropdown
            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            if (branchId.HasValue && branchId.Value > 0)
                q = q.Where(r => r.BranchId == branchId.Value);

            if (!string.IsNullOrWhiteSpace(type))
                q = q.Where(r => r.Type == type);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                q = q.Where(r =>
                    r.Car.PlateNumber.ToLower().Contains(s) ||
                    r.Car.Brand.ToLower().Contains(s) ||
                    r.Car.Model.ToLower().Contains(s));
            }

            if (from.HasValue) q = q.Where(r => r.ReminderDate >= from.Value);
            if (to.HasValue) q = q.Where(r => r.ReminderDate <= to.Value);

            if (onlyOverdue == true)
                q = q.Where(r => !r.IsCompleted && r.ReminderDate < DateTime.UtcNow.Date);

            var data = await q.OrderBy(r => r.ReminderDate).ToListAsync();
            return View(data);
        }

        // ✅ CREATE (GET)
        public async Task<IActionResult> Create()
        {
            Console.WriteLine("🟢 [DEBUG] GET /Admin/Reminders/Create triggered.");

            if (User.IsInRole("SuperAdmin"))
            {
                ViewBag.Branches = await _context.Branches.ToListAsync();
                ViewBag.Cars = await _context.Cars.Include(c => c.Branch).ToListAsync();
                Console.WriteLine("🟡 [DEBUG] SuperAdmin can see all branches and cars.");
            }
            else
            {
                var me = await _userManager.GetUserAsync(User);

                ViewBag.Cars = await _context.Cars
                    .Where(c => c.BranchId == me.BranchId)
                    .Include(c => c.Branch)
                    .ToListAsync();

                ViewBag.Branches = await _context.Branches
                    .Where(b => b.BranchId == me.BranchId)
                    .ToListAsync();

                Console.WriteLine($"🟠 [DEBUG] Admin view limited to BranchId={me?.BranchId}");
            }

            // 👇 Pre-fill date for convenience
            return View(new CarReminder { ReminderDate = DateTime.UtcNow.Date });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CarReminder reminder)
        {
            Console.WriteLine("🟣 [DEBUG] POST /Admin/Reminders/Create triggered.");
            Console.WriteLine($"🔹 Incoming Data → Type={reminder.Type}, CarId={reminder.CarId}, Date={reminder.ReminderDate}, Notes={reminder.Notes}");

            // ✅ Skip navigation props from validation (not posted from form)
            ModelState.Remove("Car");
            ModelState.Remove("Branch");
            ModelState.Remove("BranchId");

            foreach (var e in ModelState)
            {
                if (e.Value.Errors.Any())
                    Console.WriteLine($"❌ {e.Key}: {string.Join(", ", e.Value.Errors.Select(er => er.ErrorMessage))}");
            }

            Console.WriteLine($"🧩 ModelState.Valid = {ModelState.IsValid}");

            if (ModelState.IsValid)
            {
                // ✅ Auto-assign branch from the selected car
                var car = await _context.Cars
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.CarId == reminder.CarId);

                if (car == null)
                {
                    TempData["Error"] = "Selected car not found.";
                    Console.WriteLine("❌ [ERROR] Car not found for selected CarId.");
                    return RedirectToAction(nameof(Index));
                }

                reminder.BranchId = car.BranchId;
                reminder.CreatedAt = DateTime.UtcNow;

                _context.CarReminders.Add(reminder);
                await _context.SaveChangesAsync();

                Console.WriteLine("✅ [DEBUG] Reminder saved successfully.");
                TempData["Success"] = "✅ Reminder created successfully.";

                return RedirectToAction(nameof(Index));
            }

            // 🔁 Reload dropdowns if form reloads
            if (User.IsInRole("SuperAdmin"))
            {
                ViewBag.Cars = await _context.Cars.Include(c => c.Branch).ToListAsync();
            }
            else
            {
                var me = await _userManager.GetUserAsync(User);
                ViewBag.Cars = await _context.Cars
                    .Where(c => c.BranchId == me.BranchId)
                    .ToListAsync();
            }

            return View(reminder);
        }


        // EDIT (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var m = await _context.CarReminders
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.ReminderId == id);
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
        public async Task<IActionResult> Edit(int id, CarReminder input)
        {
            if (id != input.ReminderId) return NotFound();
            var m = await _context.CarReminders.FindAsync(id);
            if (m == null) return NotFound();

            if (User.IsInRole("Admin"))
            {
                var me = await _userManager.GetUserAsync(User);
                var car = await _context.Cars.FindAsync(input.CarId);
                if (me?.BranchId == null || car == null || car.BranchId != me.BranchId) return Forbid();
                m.BranchId = me.BranchId.Value;
            }
            else
            {
                var car = await _context.Cars.FindAsync(input.CarId);
                if (car == null) return BadRequest();
                m.BranchId = car.BranchId;
            }

            if (!ModelState.IsValid) return View(input);

            m.CarId = input.CarId;
            m.Type = input.Type;
            m.ReminderDate = input.ReminderDate;
            m.IsCompleted = input.IsCompleted;
            m.CompletedAt = input.IsCompleted ? (input.CompletedAt ?? DateTime.UtcNow) : null;
            m.Notes = input.Notes;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Reminder updated.";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            var m = await _context.CarReminders
                .Include(r => r.Car)
                .FirstOrDefaultAsync(r => r.ReminderId == id);

            if (m == null)
                return NotFound();

            // 🔒 Restrict branch admins to their own branch
            if (User.IsInRole("Admin"))
            {
                var me = await _userManager.GetUserAsync(User);
                if (m.BranchId != me?.BranchId)
                    return Forbid();
            }

            // ✅ Toggle completion status
            m.IsCompleted = !m.IsCompleted;
            m.CompletedAt = m.IsCompleted ? DateTime.UtcNow : null;

            // ✅ If reminder marked complete AND it's a "Service" type → add maintenance record
            if (m.IsCompleted && m.Type.Equals("Service", StringComparison.OrdinalIgnoreCase))
            {
                // Check for duplicates — same car & same service date
                bool alreadyExists = await _context.CarMaintenanceHistories
                    .AnyAsync(h =>
                        h.CarId == m.CarId &&
                        h.ServiceDate.Date == DateTime.UtcNow.Date);

                if (!alreadyExists)
                {
                    var maintenance = new CarMaintenanceHistory
                    {
                        CarId = m.CarId,
                        BranchId = m.BranchId!.Value,
                        ServiceType = m.Type,
                        ServiceDate = DateTime.UtcNow,
                        Notes = "Auto-added from completed reminder.",
                        CreatedAt = DateTime.UtcNow
                    };


                    _context.CarMaintenanceHistories.Add(maintenance);
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = m.IsCompleted
                ? "Reminder marked complete and maintenance record created (if applicable)."
                : "Reminder re-opened.";

            return RedirectToAction(nameof(Index));
        }


        // DELETE (GET)
        public async Task<IActionResult> Delete(int id)
        {
            var m = await _context.CarReminders
                .Include(r => r.Car).ThenInclude(c => c.Branch)
                .FirstOrDefaultAsync(r => r.ReminderId == id);
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
            var m = await _context.CarReminders.FindAsync(id);
            if (m != null)
            {
                if (User.IsInRole("Admin"))
                {
                    var me = await _userManager.GetUserAsync(User);
                    if (m.BranchId != me?.BranchId) return Forbid();
                }

                _context.CarReminders.Remove(m);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Reminder deleted.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}