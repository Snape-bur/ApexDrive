using ApexDrive.Data;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Services
{
    public class PricingService
    {
        private readonly ApplicationDbContext _context;

        public PricingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> CalculateBaseCost(int carId, DateTime start, DateTime end)
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null) return 0;

            var rules = await _context.PricingRules.AsNoTracking().ToListAsync();
            decimal total = 0;

            int totalDays = (end.Date - start.Date).Days;

            for (var date = start.Date; date < end.Date; date = date.AddDays(1))
            {
                // 1️⃣ Find applicable pricing rules (holiday / seasonal)
                var applicableRules = rules.Where(r =>
                    (date >= r.StartDate.Date && date <= r.EndDate.Date) ||
                    (r.IsRecurring && IsDateInRecurringRange(date, r.StartDate, r.EndDate))
                ).ToList();

                decimal multiplier;

                if (applicableRules.Any())
                {
                    // ✅ Holiday / Seasonal takes priority
                    multiplier = applicableRules.Max(r => r.Multiplier);
                }
                else if (date.DayOfWeek == DayOfWeek.Saturday ||
                         date.DayOfWeek == DayOfWeek.Sunday)
                {
                    // ✅ Weekend surge
                    multiplier = 1.1m;
                }
                else
                {
                    multiplier = 1.0m;
                }

                Console.WriteLine(
                    $"[PRICING DEBUG] Date: {date:yyyy-MM-dd}, Multiplier: {multiplier}"
                );

                total += car.DailyRate * multiplier;
            }


            // 4️⃣ LONG RENTAL DISCOUNT
            if (totalDays >= 7)
            {
                total *= 0.90m;
            }

            return decimal.Round(total, 2);
        }

        private bool IsDateInRecurringRange(DateTime current, DateTime start, DateTime end)
        {
            var check = new DateTime(2000, current.Month, current.Day);
            var s = new DateTime(2000, start.Month, start.Day);
            var e = new DateTime(2000, end.Month, end.Day);

            return s <= e
                ? check >= s && check <= e
                : check >= s || check <= e;
        }
    }
}
