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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            // 🔒 Branch security check
            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (booking.PickupBranchId != user?.BranchId &&
                    booking.Car.BranchId != user?.BranchId)
                    return Forbid();
            }

            // ✅ Prevent overlapping confirmed bookings for same car
            bool hasConflict = await _context.Bookings
                .AnyAsync(b =>
                    b.CarId == booking.CarId &&
                    b.BookingId != id &&
                    b.Status == BookingStatus.Confirmed &&
                    (
                        (booking.StartDate >= b.StartDate && booking.StartDate < b.EndDate) ||
                        (booking.EndDate > b.StartDate && booking.EndDate <= b.EndDate) ||
                        (booking.StartDate <= b.StartDate && booking.EndDate >= b.EndDate)
                    ));

            if (hasConflict)
            {
                TempData["Error"] = "⚠️ This car already has a confirmed booking for the selected dates.";
                return RedirectToAction("Index");
            }

            // ✅ Confirm booking
            booking.Status = BookingStatus.Confirmed;

            // ✅ Update related payment status
            if (booking.Payment != null)
            {
                switch (booking.Payment.Method)
                {
                    case "Card":
                    case "Credit Card":
                        booking.Payment.Status = "Paid";
                        break;

                    case "Cash":
                    case "Cash on Pick-up":
                        booking.Payment.Status = "Awaiting Cash";
                        break;

                    case "Transfer":
                    case "Bank Transfer":
                        booking.Payment.Status = "Awaiting Verification";
                        break;

                    default:
                        booking.Payment.Status = "Pending";
                        break;
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"✅ Booking #{booking.BookingId} confirmed successfully.";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return NotFound();

            // 🔒 Branch security check
            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (booking.PickupBranchId != user?.BranchId &&
                    booking.Car.BranchId != user?.BranchId)
                    return Forbid();
            }

            booking.Status = BookingStatus.Cancelled;

            if (booking.Payment != null)
                booking.Payment.Status = "Refund Pending";

            await _context.SaveChangesAsync();
            TempData["Success"] = $"❌ Booking #{booking.BookingId} cancelled.";
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Car)
                .FirstOrDefaultAsync(p => p.BookingId == id);

            if (payment == null)
            {
                TempData["Error"] = "⚠️ Payment not found for this booking.";
                return RedirectToAction(nameof(Index));
            }

            // 🔒 Branch security check
            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (payment.Booking.PickupBranchId != user?.BranchId &&
                    payment.Booking.Car.BranchId != user?.BranchId)
                    return Forbid();
            }

            if (payment.Status == "Paid")
            {
                TempData["Error"] = $"💰 Booking #{id} is already marked as paid.";
                return RedirectToAction(nameof(Index));
            }

            payment.Status = "Paid";
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✅ Booking #{id} payment marked as paid successfully.";
            return RedirectToAction(nameof(Index));
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

            var booking = await _context.Bookings
                .Include(b => b.Car)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            // 🔒 Branch security check
            if (User.IsInRole("Admin"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (booking.PickupBranchId != user?.BranchId &&
                    booking.Car.BranchId != user?.BranchId)
                    return Forbid();
            }

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
