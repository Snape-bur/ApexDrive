using ApexDrive.Data;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Services
{
    public class PricingService
    {
        private readonly ApplicationDbContext _context;

        public PricingService(ApplicationDbContext context) => _context = context;

        public async Task<decimal> CalculateBaseCost(int carId, DateTime start, DateTime end)
        {
            var car = await _context.Cars.FindAsync(carId);
            if (car == null) return 0;

            var rules = await _context.PricingRules.ToListAsync();
            decimal total = 0;

            // Calculate total rental duration
            int totalDays = (end.Date - start.Date).Days;

            for (var date = start.Date; date < end.Date; date = date.AddDays(1))
            {
                // 1. Find Admin-configured rules (Seasonal Surcharges)
                var applicableRules = rules.Where(r =>
                    (date >= r.StartDate && date <= r.EndDate) ||
                    (r.IsRecurring && IsDateInRecurringRange(date, r.StartDate, r.EndDate))
                ).ToList();

                decimal dailyMultiplier;

                if (applicableRules.Any())
                {
                    // Use highest multiplier for intelligent profit management
                    dailyMultiplier = applicableRules.Max(r => r.Multiplier);
                }
                else if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                {
                    // 2. Automatic Weekend Surge (Smart Feature)
                    dailyMultiplier = 1.1m;
                }
                else
                {
                    dailyMultiplier = 1.0m;
                }

                total += (car.DailyRate * dailyMultiplier);
            }

            // 3. SMART FEATURE: Duration Discounts
            // If rental is 7 days or longer, apply a 10% discount to the final cost
            if (totalDays >= 7)
            {
                total *= 0.90m;
            }

            return total;
        }

        private bool IsDateInRecurringRange(DateTime current, DateTime start, DateTime end)
        {
            var checkDate = new DateTime(2000, current.Month, current.Day);
            var startDate = new DateTime(2000, start.Month, start.Day);
            var endDate = new DateTime(2000, end.Month, end.Day);

            if (startDate <= endDate)
                return checkDate >= startDate && checkDate <= endDate;

            return checkDate >= startDate || checkDate <= endDate;
        }
    }
}
