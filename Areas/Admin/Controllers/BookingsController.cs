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
    public class BookingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public BookingsController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ Index: list all bookings with search + branch filter
        public async Task<IActionResult> Index(string search, int? branchId, BookingStatus? status)
        {
            var query = _context.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Branch)
                .Include(b => b.Customer)
                .Include(b => b.Payment)
                .Include(b => b.PickupBranch)
                .Include(b => b.DropoffBranch)
                .AsQueryable();

            // 🔹 Admin → only their branch
            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin") && currentUser?.BranchId != null)
            {
                query = query.Where(b =>
                    b.PickupBranchId == currentUser.BranchId ||
                    b.Car.BranchId == currentUser.BranchId);
            }

            // 🔹 SuperAdmin → optional branch filter
            if (User.IsInRole("SuperAdmin"))
                ViewBag.Branches = await _context.Branches.ToListAsync();

            if (branchId.HasValue && branchId.Value > 0)
            {
                query = query.Where(b =>
                    b.PickupBranchId == branchId || b.Car.BranchId == branchId);
            }

            // 🔍 search filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(b =>
                    b.Customer.Email.ToLower().Contains(search) ||
                    b.Car.PlateNumber.ToLower().Contains(search) ||
                    b.Car.Brand.ToLower().Contains(search) ||
                    b.Car.Model.ToLower().Contains(search));
            }

            // 🟢 status filter
            if (status.HasValue)
                query = query.Where(b => b.Status == status);

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // ✅ View booking details
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car).ThenInclude(c => c.Branch)
                .Include(b => b.Customer)
                .Include(b => b.Payment)
                .Include(b => b.PickupBranch)
                .Include(b => b.DropoffBranch)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            // ✅ restrict branch admins
            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Admin") &&
                currentUser?.BranchId != null &&
                booking.PickupBranchId != currentUser.BranchId &&
                booking.Car.BranchId != currentUser.BranchId)
            {
                return Forbid();
            }

            return View(booking);
        }

        // ✅ Confirm booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking #{booking.BookingId} confirmed successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Cancel booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking #{booking.BookingId} cancelled.";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Mark as completed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.Status = BookingStatus.Completed;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking #{booking.BookingId} marked as completed.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Edit(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.PickupBranch)
                .Include(b => b.DropoffBranch)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            ViewBag.Branches = await _context.Branches.ToListAsync();
            return View(booking);
        }

        // POST: Edit Booking
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id, Booking updated)
        {
            if (id != updated.BookingId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Branches = await _context.Branches.ToListAsync();
                return View(updated);
            }

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            booking.StartDate = updated.StartDate;
            booking.EndDate = updated.EndDate;
            booking.PickupBranchId = updated.PickupBranchId;
            booking.DropoffBranchId = updated.DropoffBranchId;
            booking.BaseCost = updated.BaseCost;
            booking.TotalCost = updated.TotalCost;
            booking.Status = updated.Status;

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Booking #{booking.BookingId} updated successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}
