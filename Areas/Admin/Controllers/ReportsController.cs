using ApexDrive.Areas.Admin.ViewModels.Reports;
using ApexDrive.Data;
using ApexDrive.Models;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Font = iTextSharp.text.Font;


namespace ApexDrive.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ReportsController(ApplicationDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🧭 Index page for report options
        public IActionResult Index() => View();

        // ✅ Booking Report View
        public async Task<IActionResult> Bookings(DateTime? startDate, DateTime? endDate)
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

            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Car).ThenInclude(c => c.Branch)
                .Include(b => b.PickupBranch)
                .Include(b => b.DropoffBranch)
                .AsQueryable();

            if (branchId.HasValue)
                query = query.Where(b => b.PickupBranchId == branchId || b.Car.BranchId == branchId);

            if (startDate.HasValue)
                query = query.Where(b => b.StartDate >= startDate);
            if (endDate.HasValue)
                query = query.Where(b => b.EndDate <= endDate);

            var data = await query.ToListAsync();
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            return View(data);
        }
        //  Vehicle Utilization Report
        public async Task<IActionResult> VehicleUtilization(DateTime? fromDate, DateTime? toDate)
        {
            // ✅ Default to LAST 30 DAYS (realistic)
            fromDate ??= DateTime.UtcNow.Date.AddDays(-30);
            toDate ??= DateTime.UtcNow.Date;

            var totalDays = (toDate.Value - fromDate.Value).Days + 1;

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

            //  Cars
            var carsQuery = _context.Cars
                .Include(c => c.Branch)
                .AsQueryable();

            if (branchId.HasValue)
                carsQuery = carsQuery.Where(c => c.BranchId == branchId);

            var cars = await carsQuery.ToListAsync();

            //  Bookings (IMPORTANT FIX)
            var bookingsQuery = _context.Bookings
                .Include(b => b.Car)
                .Where(b =>
                    b.StartDate <= toDate.Value &&
                    b.EndDate >= fromDate.Value);

            if (branchId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b =>
                    b.Car.BranchId == branchId ||
                    b.PickupBranchId == branchId);
            }

            var bookings = await bookingsQuery.ToListAsync();

            var items = cars.Select(car =>
            {
                var carBookings = bookings.Where(b => b.CarId == car.CarId).ToList();

                var rentedDays = carBookings.Sum(b =>
                {
                    var actualStart = b.StartDate < fromDate ? fromDate.Value : b.StartDate;
                    var actualEnd = b.EndDate > toDate ? toDate.Value : b.EndDate;

                    return actualEnd >= actualStart
                        ? (actualEnd - actualStart).Days + 1
                        : 0;
                });

                var utilization = totalDays > 0
                    ? Math.Round((decimal)rentedDays / totalDays * 100, 2)
                    : 0;

                return new VehicleUtilizationItemVM
                {
                    CarId = car.CarId,
                    PlateNumber = car.PlateNumber,
                    BranchName = car.Branch.BranchName,
                    TotalBookings = carBookings.Count,
                    TotalRentedDays = rentedDays,
                    UtilizationRate = utilization
                };
            })
            .OrderByDescending(x => x.UtilizationRate)
            .ToList();

            var model = new VehicleUtilizationReportVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                Vehicles = items
            };

            return View(model);
        }

        //  Maintenance Due Report
        public async Task<IActionResult> MaintenanceDue(DateTime? fromDate, DateTime? toDate)
        {
            // Default date range (used only for UI display)
            fromDate ??= DateTime.UtcNow.Date;
            toDate ??= DateTime.UtcNow.Date.AddDays(30);

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

            // ✅ IMPORTANT: include overdue + upcoming (no fromDate filter)
            var remindersQuery = _context.CarReminders
                .Include(r => r.Car)
                    .ThenInclude(c => c.Branch)
                .Where(r =>
                    !r.IsCompleted &&
                    r.ReminderDate.Date <= toDate.Value);

            if (branchId.HasValue)
            {
                remindersQuery = remindersQuery.Where(r =>
                    r.Car.BranchId == branchId);
            }

            var items = await remindersQuery
                .OrderBy(r => r.ReminderDate)
                .Select(r => new MaintenanceDueItemVM
                {
                    ReminderId = r.ReminderId,
                    CarPlate = r.Car.PlateNumber,
                    BranchName = r.Car.Branch.BranchName,
                    MaintenanceType = r.Type,
                    DueDate = r.ReminderDate,
                    IsOverdue = r.ReminderDate.Date < DateTime.UtcNow.Date
                })
                .ToListAsync();

            var model = new MaintenanceDueReportVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                TotalDue = items.Count,
                OverdueCount = items.Count(x => x.IsOverdue),
                Items = items
            };

            return View(model);
        }



        public async Task<IActionResult> Revenue(DateTime? fromDate, DateTime? toDate)
        {
            // Default range: last 30 days
            fromDate ??= DateTime.UtcNow.Date.AddDays(-30);
            toDate ??= DateTime.UtcNow.Date;

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

            // ✅ Unified date logic
            var start = fromDate.Value.Date;
            var end = toDate.Value.Date.AddDays(1).AddTicks(-1);

            // ✅ Unified revenue logic
            var paymentsQuery = _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Customer)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Car)
                .Include(p => p.Booking)
                    .ThenInclude(b => b.PickupBranch)
                .Where(p => p.CreatedAt >= start && p.CreatedAt <= end);

            // ✅ Admin sees only own branch revenue
            if (branchId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(p =>
                    p.Booking.PickupBranchId == branchId);
            }

            var items = await paymentsQuery
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new RevenueItemVM
                {
                    BookingId = p.BookingId,
                    CustomerName = p.Booking.Customer.FullName,
                    CarPlate = p.Booking.Car.PlateNumber,
                    PaymentDate = p.CreatedAt,
                    Amount = p.Amount
                })
                .ToListAsync();

            return View(new RevenueReportVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                TotalRevenue = items.Sum(x => x.Amount),
                Items = items
            });
        }



        [Authorize(Roles = "Admin,SuperAdmin")]

        public async Task<FileResult> ExportMaintenanceDueExcel()
        {
            var today = DateTime.UtcNow.Date;

            var data = await _context.CarMaintenanceHistories
                .Include(m => m.Car)
                .Include(m => m.Branch)
                .Where(m =>
                    m.Car.LastServiceDate.HasValue &&
                    m.Car.LastServiceDate.Value.AddMonths(6) <= today
                )
                .OrderBy(m => m.Car.LastServiceDate)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Maintenance Due");

            ws.Cell(1, 1).Value = "Car Plate";
            ws.Cell(1, 2).Value = "Branch";
            ws.Cell(1, 3).Value = "Last Service Date";
            ws.Cell(1, 4).Value = "Next Service Date";

            int row = 2;
            foreach (var m in data)
            {
                var lastService = m.Car.LastServiceDate!.Value;
                var nextService = lastService.AddMonths(6);

                ws.Cell(row, 1).Value = m.Car.PlateNumber;
                ws.Cell(row, 2).Value = m.Branch?.BranchName ?? "-";
                ws.Cell(row, 3).Value = lastService.ToShortDateString();
                ws.Cell(row, 4).Value = nextService.ToShortDateString();
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "MaintenanceDue.xlsx"
            );
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<FileResult> ExportMaintenanceDuePdf()
        {
            var today = DateTime.Today;

            var data = await _context.CarMaintenanceHistories
                .Include(m => m.Car)
                .Include(m => m.Branch)
                .Where(m => m.ServiceDate.AddMonths(6) <= today)
                .OrderBy(m => m.ServiceDate)
                .ToListAsync();

            using var stream = new MemoryStream();
            var doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, stream);
            doc.Open();

            var title = new Paragraph(
                "Maintenance Due Report\n\n",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)
            )
            {
                Alignment = Element.ALIGN_CENTER
            };
            doc.Add(title);

            var table = new PdfPTable(4) { WidthPercentage = 100 };
            table.AddCell("Car Plate");
            table.AddCell("Branch");
            table.AddCell("Last Service");
            table.AddCell("Next Service");

            foreach (var m in data)
            {
                var lastService = m.ServiceDate;
                var nextService = lastService.AddMonths(6);

                table.AddCell(m.Car?.PlateNumber ?? "-");
                table.AddCell(m.Branch?.BranchName ?? "-");
                table.AddCell(lastService.ToString("dd MMM yyyy"));
                table.AddCell(nextService.ToString("dd MMM yyyy"));
            }

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "MaintenanceDue.pdf");
        }



        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> RevenueByBranch(DateTime? fromDate, DateTime? toDate)
        {
            fromDate ??= DateTime.UtcNow.Date.AddDays(-30);
            toDate ??= DateTime.UtcNow.Date;

            var data = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.Car)
                        .ThenInclude(c => c.Branch)
                .Where(p =>
                    p.CreatedAt.Date >= fromDate &&
                    p.CreatedAt.Date <= toDate)
                .GroupBy(p => new
                {
                    p.Booking.Car.Branch.BranchId,
                    p.Booking.Car.Branch.BranchName
                })
                .Select(g => new RevenueByBranchItemVM
                {
                    BranchId = g.Key.BranchId,
                    BranchName = g.Key.BranchName,
                    TotalRevenue = g.Sum(x => x.Amount),
                    TotalBookings = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToListAsync();

            var model = new RevenueByBranchReportVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value,
                GrandTotalRevenue = data.Sum(x => x.TotalRevenue),
                Branches = data
            };

            return View(model);
        }



        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> BranchPerformance(DateTime? fromDate, DateTime? toDate)
        {
            // Default range: last 30 days
            fromDate ??= DateTime.UtcNow.Date.AddDays(-30);
            toDate ??= DateTime.UtcNow.Date;

            var start = fromDate.Value.Date;
            var end = toDate.Value.Date.AddDays(1).AddTicks(-1);

            // Load once (performance-safe)
            var branches = await _context.Branches.ToListAsync();

            var payments = await _context.Payments
                .Include(p => p.Booking)
                    .ThenInclude(b => b.PickupBranch)
                .Where(p => p.CreatedAt >= start && p.CreatedAt <= end)
                .ToListAsync();

            var bookings = await _context.Bookings
                .Where(b => b.StartDate <= end && b.EndDate >= start)
                .ToListAsync();

            var cars = await _context.Cars.ToListAsync();

            var model = new BranchPerformanceReportVM
            {
                FromDate = fromDate.Value,
                ToDate = toDate.Value
            };

            foreach (var branch in branches)
            {
                model.Branches.Add(new BranchPerformanceItemVM
                {
                    BranchId = branch.BranchId,
                    BranchName = branch.BranchName,

                    // Static asset
                    TotalCars = cars.Count(c => c.BranchId == branch.BranchId),

                    // Period-based KPIs
                    TotalBookings = bookings
                        .Count(b => b.PickupBranchId == branch.BranchId),

                    TotalRevenue = payments
                        .Where(p => p.Booking.PickupBranchId == branch.BranchId)
                        .Sum(p => p.Amount)
                });
            }

            return View(model);
        }



        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<FileResult> ExportBookingsExcel()
        {
            var user = await _userManager.GetUserAsync(User);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            var query = _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .Include(b => b.PickupBranch)
                .AsQueryable();

            // 🔒 Branch Admin → only their branch
            if (!isSuperAdmin)
            {
                if (!user.BranchId.HasValue)
                    throw new UnauthorizedAccessException("Branch admin has no branch assigned.");

                query = query.Where(b => b.PickupBranchId == user.BranchId);
            }

            var bookings = await query.ToListAsync();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Bookings Report");

            ws.Cell(1, 1).Value = "Booking ID";
            ws.Cell(1, 2).Value = "Customer";
            ws.Cell(1, 3).Value = "Car";
            ws.Cell(1, 4).Value = "Pickup Branch";
            ws.Cell(1, 5).Value = "Start Date";
            ws.Cell(1, 6).Value = "End Date";
            ws.Cell(1, 7).Value = "Status";
            ws.Cell(1, 8).Value = "Total Cost";

            int row = 2;
            foreach (var b in bookings)
            {
                ws.Cell(row, 1).Value = b.BookingId;
                ws.Cell(row, 2).Value = b.Customer?.FullName ?? b.Customer?.Email;
                ws.Cell(row, 3).Value = b.Car?.PlateNumber;
                ws.Cell(row, 4).Value = b.PickupBranch?.BranchName;
                ws.Cell(row, 5).Value = b.StartDate.ToString("dd MMM yyyy");
                ws.Cell(row, 6).Value = b.EndDate.ToString("dd MMM yyyy");
                ws.Cell(row, 7).Value = b.Status.ToString();
                ws.Cell(row, 8).Value = b.TotalCost;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BookingsReport.xlsx"
            );
        }


        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<FileResult> ExportBookingsPdf()
        {
            var user = await _userManager.GetUserAsync(User);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            var query = _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .Include(b => b.PickupBranch)
                .AsQueryable();

            if (!isSuperAdmin)
            {
                if (!user.BranchId.HasValue)
                    throw new UnauthorizedAccessException("Branch admin has no branch assigned.");

                query = query.Where(b => b.PickupBranchId == user.BranchId);
            }

            var bookings = await query.ToListAsync();

            using var stream = new MemoryStream();
            var doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, stream);
            doc.Open();

            var title = new Paragraph(
                isSuperAdmin ? "All Branches – Booking Report\n\n"
                             : "Branch Booking Report\n\n",
                FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16)
            )
            {
                Alignment = Element.ALIGN_CENTER
            };
            doc.Add(title);

            var table = new PdfPTable(6) { WidthPercentage = 100 };
            table.AddCell("Booking ID");
            table.AddCell("Customer");
            table.AddCell("Car");
            table.AddCell("Pickup Branch");
            table.AddCell("Start Date");
            table.AddCell("Status");

            foreach (var b in bookings)
            {
                table.AddCell(b.BookingId.ToString());
                table.AddCell(b.Customer?.FullName ?? b.Customer?.Email ?? "-");
                table.AddCell(b.Car?.PlateNumber ?? "-");
                table.AddCell(b.PickupBranch?.BranchName ?? "-");
                table.AddCell(b.StartDate.ToString("dd MMM yyyy"));
                table.AddCell(b.Status.ToString());
            }

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "BookingsReport.pdf");
        }

    }
}

