using ApexDrive.Data;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Authorization;
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

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
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

        // ✅ Export to Excel
        public async Task<FileResult> ExportBookingsExcel()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .Include(b => b.PickupBranch)
                .ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bookings Report");

            worksheet.Cell(1, 1).Value = "Booking ID";
            worksheet.Cell(1, 2).Value = "Customer";
            worksheet.Cell(1, 3).Value = "Car";
            worksheet.Cell(1, 4).Value = "Pickup Branch";
            worksheet.Cell(1, 5).Value = "Start Date";
            worksheet.Cell(1, 6).Value = "End Date";
            worksheet.Cell(1, 7).Value = "Status";
            worksheet.Cell(1, 8).Value = "Total Cost";

            int row = 2;
            foreach (var b in bookings)
            {
                worksheet.Cell(row, 1).Value = b.BookingId;
                worksheet.Cell(row, 2).Value = b.Customer?.FullName ?? b.Customer?.Email;
                worksheet.Cell(row, 3).Value = b.Car?.PlateNumber;
                worksheet.Cell(row, 4).Value = b.PickupBranch?.BranchName;
                worksheet.Cell(row, 5).Value = b.StartDate.ToShortDateString();
                worksheet.Cell(row, 6).Value = b.EndDate.ToShortDateString();
                worksheet.Cell(row, 7).Value = b.Status.ToString();
                worksheet.Cell(row, 8).Value = b.TotalCost;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "BookingsReport.xlsx");
        }

        // ✅ Export to PDF
        public async Task<FileResult> ExportBookingsPdf()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Customer)
                .Include(b => b.PickupBranch)
                .ToListAsync();

            using var stream = new MemoryStream();
            var doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, stream);
            doc.Open();

            var baseFont = BaseFont.CreateFont(BaseFont.HELVETICA, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
            var titleFont = new Font(baseFont, 16, Font.BOLD);
            var title = new Paragraph("ApexDrive - Booking Report\n\n", titleFont)
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
                table.AddCell(b.StartDate.ToShortDateString());
                table.AddCell(b.Status.ToString());
            }

            doc.Add(table);
            doc.Close();

            return File(stream.ToArray(), "application/pdf", "BookingsReport.pdf");

        }
    }
}
