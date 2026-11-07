using Microsoft.EntityFrameworkCore;
using NWSDB.API.Data;
using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public class WaterUsageService : IWaterUsageService
    {
        private readonly ApplicationDbContext _context;

        public WaterUsageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<WaterUsage>> GetAllUsageAsync()
        {
            return await _context.WaterUsages
                .OrderByDescending(u => u.ReadingDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<WaterUsage>> GetUsageByCustomerIdAsync(string customerId)
        {
            return await _context.WaterUsages
                .Where(u => u.CustomerId == customerId)
                .OrderByDescending(u => u.ReadingDate)
                .ToListAsync();
        }

        public async Task<WaterUsage?> GetLatestUsageAsync(string customerId)
        {
            return await _context.WaterUsages
                .Where(u => u.CustomerId == customerId)
                .OrderByDescending(u => u.ReadingDate)
                .FirstOrDefaultAsync();
        }

        public async Task<WaterUsage?> GetUsageForPeriodAsync(string customerId, string billingPeriod)
        {
            return await _context.WaterUsages
                .FirstOrDefaultAsync(u => u.CustomerId == customerId && u.BillingPeriod == billingPeriod);
        }

        // Single source of truth for "which of this customer's recorded meter readings
        // still need a bill": a meter reading counts as unbilled when no Bill row exists
        // for the same CustomerId + BillingPeriod. Expressed as one NOT EXISTS query
        // against the actual WaterUsages/Bills tables - not a separate list, not hard-coded,
        // and not dependent on IBillService (so a problem with the wider Bill read path
        // can never prevent Staff from seeing which readings are still unbilled).
        public async Task<IEnumerable<WaterUsage>> GetUnbilledUsageAsync(string customerId)
        {
            return await _context.WaterUsages
                .Where(u => u.CustomerId == customerId &&
                    !_context.Bills.Any(b => b.CustomerId == u.CustomerId && b.BillingPeriod == u.BillingPeriod))
                .OrderByDescending(u => u.ReadingDate)
                .ToListAsync();
        }

        public async Task<WaterUsage> CreateUsageAsync(WaterUsage usage)
        {
            _context.WaterUsages.Add(usage);
            await _context.SaveChangesAsync();
            return usage;
        }

        public async Task<(bool Success, string Message, WaterUsage? Usage)> RecordUsageAsync(
            string customerId, string billingPeriod, decimal unitsConsumed, DateTime readingDate)
        {
            customerId = (customerId ?? string.Empty).Trim();
            billingPeriod = (billingPeriod ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(billingPeriod))
                return (false, "Customer ID and billing period are required.", null);

            // 1. Customer must exist.
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!customerExists)
                return (false, "Customer not found.", null);

            // 2. Units consumed cannot be negative.
            if (unitsConsumed < 0)
                return (false, "Units consumed cannot be negative.", null);

            // 3. Duplicate meter reading prevention: at most one reading per customer + billing period.
            var existing = await GetUsageForPeriodAsync(customerId, billingPeriod);
            if (existing != null)
                return (false, "A meter reading has already been recorded for this customer and billing period.", null);

            var usage = new WaterUsage
            {
                CustomerId = customerId,
                BillingPeriod = billingPeriod,
                UnitsConsumed = unitsConsumed,
                ReadingDate = readingDate
            };

            var created = await CreateUsageAsync(usage); // reuse the existing persistence method
            return (true, "Meter reading recorded successfully.", created);
        }
    }
}
