using System;
using System.Linq;
using System.Threading.Tasks;
using ApexDrive.Data;
using ApexDrive.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ INDEX — Advanced Filter
        public async Task<IActionResult> Index(string search, string status, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Customer)
                .AsQueryable();

            // 🔍 Search: BookingId or Customer Email
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                query = query.Where(p =>
                    p.Booking.BookingId.ToString().Contains(search) ||
                    p.Booking.Customer.Email.ToLower().Contains(search));
            }

            // 🟡 Status filter
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.Where(p => p.Status == status);
            }

            // 📅 Date filter
            if (fromDate.HasValue)
                query = query.Where(p => p.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(p => p.CreatedAt <= toDate.Value);

            var data = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(data);
        }

        // ✅ EDIT (GET)
        public async Task<IActionResult> Edit(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking)
                .ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // ✅ EDIT (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Payment updated)
        {
            if (id != updated.PaymentId)
                return NotFound();

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
                return NotFound();

            payment.Method = updated.Method;
            payment.Status = updated.Status;
            payment.TransactionRef = updated.TransactionRef;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Payment updated successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
