using ApexDrive.Data;
using ApexDrive.Models;
using ApexDrive.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApexDrive.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize(Roles = "Customer")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly PricingService _pricingService;

        public BookingController(ApplicationDbContext context, UserManager<AppUser> userManager, PricingService pricingService)
        {
            _context = context;
            _userManager = userManager;
            _pricingService = pricingService;
        }

        // ✅ STEP 1: Booking Form
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Branches = await _context.Branches.ToListAsync();
            return View();
        }
        // ✅ Handle Search submission from homepage form
        [HttpPost]
        [AllowAnonymous] // Important so guests can search before login
        [ValidateAntiForgeryToken]
        public IActionResult Search(int PickupBranchId, int DropoffBranchId, DateTime StartDate, DateTime EndDate)
        {
            // Store booking data temporarily
            var booking = new Booking
            {
                PickupBranchId = PickupBranchId,
                DropoffBranchId = DropoffBranchId,
                StartDate = StartDate,
                EndDate = EndDate
            };

            TempData["BookingData"] = System.Text.Json.JsonSerializer.Serialize(booking);

            // Redirect to next step (Step2_SelectCar)
            return RedirectToAction("Step2_SelectCar", "Booking", new { area = "Customer" });
        }



        // ✅ STEP 2: Select Car (with dynamic pricing applied)
        public async Task<IActionResult> Step2_SelectCar()
        {
            var temp = TempData["BookingData"] as string;
            if (string.IsNullOrWhiteSpace(temp))
                return RedirectToAction("Index", "Home", new { area = "" });

            TempData.Keep("BookingData");
            var booking = JsonSerializer.Deserialize<Booking>(temp);
            if (booking == null)
                return RedirectToAction("Index", "Home", new { area = "" });

            ViewBag.PickupBranchId = booking.PickupBranchId;
            ViewBag.DropoffBranchId = booking.DropoffBranchId;
            ViewBag.StartDate = booking.StartDate;
            ViewBag.EndDate = booking.EndDate;

            var availableCars = await _context.Cars
                .Include(c => c.Branch)
                .Where(c => c.BranchId == booking.PickupBranchId && c.IsAvailable)
                .ToListAsync();

            // ✅ Calculate dynamic price per car (holiday / weekend rules applied)
            var carPrices = new Dictionary<int, decimal>();

            foreach (var car in availableCars)
            {
                carPrices[car.CarId] = await _pricingService.CalculateBaseCost(
                    car.CarId,
                    booking.StartDate,
                    booking.EndDate
                );
            }

            // Pass calculated prices to the view
            ViewBag.CarPrices = carPrices;

            return View(availableCars);
        }


        // ✅ STEP 2 (POST): Handle car selection and move to Step 3
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SelectCar(int carId, int pickupBranchId, int dropoffBranchId, DateTime startDate, DateTime endDate)
        {
            Console.WriteLine("=== STEP 2: SelectCar Triggered ===");
            Console.WriteLine($"carId: {carId}, pickupBranchId: {pickupBranchId}, dropoffBranchId: {dropoffBranchId}");
            Console.WriteLine($"StartDate: {startDate}, EndDate: {endDate}");

            var booking = new Booking
            {
                CarId = carId,
                PickupBranchId = pickupBranchId,
                DropoffBranchId = dropoffBranchId,
                StartDate = startDate,
                EndDate = endDate
            };

            TempData["BookingData"] = JsonSerializer.Serialize(booking);

            // Redirect explicitly to Step 3 (extras) in Customer area
            return RedirectToAction("Step3_Extras", "Booking", new { area = "Customer", carId });
        }


        // ✅ STEP 3: Insurance & Extras
        public class BookingExtrasViewModel
        {
            public int CarId { get; set; }
            public string InsuranceType { get; set; } = "Basic";
            public bool HasChildSeat { get; set; } = false;

            public string CarDisplayName { get; set; } = string.Empty;
            public decimal CarDailyRate { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public int PickupBranchId { get; set; }
            public int DropoffBranchId { get; set; }
        }

        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

        [HttpGet]
        public async Task<IActionResult> Step3_Extras(int carId)
        {
            var temp = TempData["BookingData"] as string;
            if (string.IsNullOrWhiteSpace(temp))
                return RedirectToAction("Index", "Home", new { area = "" });

            TempData.Keep("BookingData");
            var booking = JsonSerializer.Deserialize<Booking>(temp, JsonOpts);
            if (booking == null) return RedirectToAction("Index", "Home", new { area = "" });

            var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.CarId == carId);
            if (car == null) return NotFound();

            booking.CarId = carId;
            TempData["BookingData"] = JsonSerializer.Serialize(booking);

            var vm = new BookingExtrasViewModel
            {
                CarId = carId,
                CarDisplayName = $"{car.Brand} {car.Model}",
                CarDailyRate = car.DailyRate,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                PickupBranchId = booking.PickupBranchId,
                DropoffBranchId = booking.DropoffBranchId
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step3_Extras(BookingExtrasViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var temp = TempData["BookingData"] as string;
            if (string.IsNullOrWhiteSpace(temp))
                return RedirectToAction("Index", "Home", new { area = "" });

            var booking = JsonSerializer.Deserialize<Booking>(temp, JsonOpts);
            if (booking == null) return RedirectToAction("Index", "Home", new { area = "" });

            var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.CarId == model.CarId);
            if (car == null) return NotFound();

            decimal insuranceRatePerDay = model.InsuranceType switch
            {

                "Standard" => 350m,   
                "Premium" => 700m,    
                _ => 0m
            };
            const decimal childSeatRatePerDay = 250m;

            var rawDays = (booking.EndDate.Date - booking.StartDate.Date).TotalDays;
            var days = Math.Max(1, (int)Math.Ceiling(rawDays));

            booking.CarId = model.CarId;
            booking.InsuranceType = model.InsuranceType;
            booking.HasChildSeat = model.HasChildSeat;

            // Inject PricingService into your constructor first
            booking.BaseCost = await _pricingService.CalculateBaseCost(model.CarId, booking.StartDate, booking.EndDate);
            booking.InsuranceCost = days * insuranceRatePerDay;
            booking.ChildSeatCost = model.HasChildSeat ? (days * childSeatRatePerDay) : 0m;
            booking.ExtrasCost = booking.InsuranceCost + booking.ChildSeatCost;
            booking.TotalCost = booking.BaseCost + booking.ExtrasCost;

            TempData["BookingData"] = JsonSerializer.Serialize(booking);
            return RedirectToAction(nameof(Step4_Payment));
        }

        // ✅ STEP 4: Payment Summary
        [HttpGet]
        public async Task<IActionResult> Step4_Payment()
        {
            var temp = TempData["BookingData"] as string;
            if (string.IsNullOrWhiteSpace(temp))
                return RedirectToAction("Index", "Home", new { area = "" });

            TempData.Keep("BookingData");
            var booking = JsonSerializer.Deserialize<Booking>(temp, JsonOpts);
            if (booking == null)
                return RedirectToAction("Index", "Home", new { area = "" });

            // ✅ Load related data
            booking.Car = await _context.Cars.FindAsync(booking.CarId);
            booking.PickupBranch = await _context.Branches.FindAsync(booking.PickupBranchId);
            booking.DropoffBranch = await _context.Branches.FindAsync(booking.DropoffBranchId);

            // ✅ NEW: Load logged-in customer info (AppUser)
            booking.Customer = await _userManager.GetUserAsync(User);

            return View(booking);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Step4_PaymentConfirm(string PaymentMethod)
        {
            // ✅ Keep the booking data alive during POST
            TempData.Keep("BookingData");

            // ✅ Debug log (shows in Visual Studio Output window)
            Console.WriteLine($"Payment method selected: {PaymentMethod}");

            var temp = TempData["BookingData"] as string;
            if (string.IsNullOrWhiteSpace(temp))
                return RedirectToAction("Index", "Home", new { area = "" });

            var booking = JsonSerializer.Deserialize<Booking>(temp, JsonOpts);
            if (booking == null)
                return RedirectToAction("Index", "Home", new { area = "" });

            var userId = _userManager.GetUserId(User);
            booking.CustomerId = userId;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // ✅ Use the selected payment method
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                Amount = booking.TotalCost,
                CreatedAt = DateTime.UtcNow,
                Status = "Pending",
                Method = PaymentMethod switch
                {
                    "Card" => "Credit / Debit Card",
                    "Transfer" => "Bank Transfer",
                    "Cash" => "Cash on Pick-up",
                    _ => "Credit / Debit Card"
                }
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData.Remove("BookingData");

            return RedirectToAction(nameof(BookingConfirmed), new { id = booking.BookingId });
        }



        // ✅ STEP 4: Confirmation Page
      
        [HttpGet]
        public async Task<IActionResult> BookingConfirmed(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.PickupBranch)
                .Include(b => b.DropoffBranch)
                .Include(b => b.Payment)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
                return RedirectToAction("Index", "Home", new { area = "" });

            var payment = await _context.Payments
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.BookingId == id);

            var summary = new
            {
                BookingId = booking.BookingId,
                Status = booking.Status.ToString(),
                CarName = $"{booking.Car?.Brand} {booking.Car?.Model}",
                PickupBranch = booking.PickupBranch?.BranchName,
                DropoffBranch = booking.DropoffBranch?.BranchName,
                StartDate = booking.StartDate.ToString("dd MMM yyyy"),
                EndDate = booking.EndDate.ToString("dd MMM yyyy"),
                BaseCost = booking.BaseCost,
                ExtrasCost = booking.ExtrasCost,
                TotalCost = booking.TotalCost,
                PaymentMethod = payment?.Method ?? "N/A",
                PaymentStatus = payment?.Status ?? "Pending"
            };

            return View(summary);
        }


        // ✅ STEP 5: Customer Booking History
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Include(b => b.Car)
                .Include(b => b.Payment)
                .Include(b => b.PickupBranch)
                .Include(b => b.DropoffBranch)
                .Where(b => b.CustomerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            // Get the booking record including related data
            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction(nameof(MyBookings));
            }

            // Ensure that only the owner can cancel their booking
            var userId = _userManager.GetUserId(User);
            if (booking.CustomerId != userId)
            {
                TempData["Error"] = "You are not authorized to cancel this booking.";
                return RedirectToAction(nameof(MyBookings));
            }

            // Only allow cancel if still pending
            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "Only pending bookings can be cancelled.";
                return RedirectToAction(nameof(MyBookings));
            }

            // ✅ Mark the booking as cancelled
            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAt = DateTime.UtcNow;

            // ✅ Optionally update payment record if exists
            if (booking.Payment != null)
            {
                booking.Payment.Status = "Refund Pending";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Your booking has been cancelled successfully.";
            return RedirectToAction(nameof(MyBookings));
        }


    }
}
