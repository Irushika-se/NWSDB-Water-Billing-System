using Microsoft.EntityFrameworkCore;
using NWSDB.API.Data;
using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public class BillService : IBillService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWaterUsageService _usageService;
        private readonly IBillingCalculationService _billingCalc;

        public BillService(
            ApplicationDbContext context,
            IWaterUsageService usageService,
            IBillingCalculationService billingCalc)
        {
            _context = context;
            _usageService = usageService;
            _billingCalc = billingCalc;
        }

        public async Task<IEnumerable<Bill>> GetAllBillsAsync()
        {
            return await _context.Bills
                .OrderByDescending(b => b.DueDate)
                .ToListAsync();
        }

        public async Task<Bill?> GetBillByIdAsync(int billId)
        {
            return await _context.Bills.FindAsync(billId);
        }

        public async Task<IEnumerable<Bill>> GetBillsByCustomerIdAsync(string customerId)
        {
            return await _context.Bills
                .Where(b => b.CustomerId == customerId)
                .OrderByDescending(b => b.DueDate)
                .ToListAsync();
        }

        public async Task<Bill?> GetCurrentBillAsync(string customerId)
        {
            // "Current bill" = the oldest unpaid bill for that customer.
            return await _context.Bills
                .Where(b => b.CustomerId == customerId && !b.IsPaid)
                .OrderBy(b => b.DueDate)
                .ThenBy(b => b.BillId)
                .FirstOrDefaultAsync();
        }

        public async Task<Bill> CreateBillAsync(Bill bill)
        {
            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();
            return bill;
        }

        public async Task<(bool Success, string Message, Bill? Bill, decimal? UnitsConsumed)> GenerateBillAsync(
            string customerId, string billingPeriod)
        {
            customerId = (customerId ?? string.Empty).Trim();
            billingPeriod = (billingPeriod ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(customerId) || string.IsNullOrWhiteSpace(billingPeriod))
                return (false, "Customer ID and billing period are required.", null, null);

            // 1. Customer must exist.
            var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == customerId);
            if (!customerExists)
                return (false, $"Customer {customerId} does not exist.", null, null);

            // 2. Duplicate bill prevention: at most one bill per customer + billing period.
            var alreadyBilled = await _context.Bills
                .AnyAsync(b => b.CustomerId == customerId && b.BillingPeriod == billingPeriod);
            if (alreadyBilled)
                return (false, "A bill already exists for this customer and billing period.", null, null);

            
            var usage = await _usageService.GetUsageForPeriodAsync(customerId, billingPeriod);
            if (usage == null)
            {
                return (false,
                    "No meter reading has been recorded for this customer and billing period yet. " +
                    "Record the meter reading first, then generate the bill.",
                    null, null);
            }

            
            decimal amount;
            var issuedDate = DateTime.Now;
            DateTime dueDate;
            try
            {
                amount = _billingCalc.CalculateAmount(usage.UnitsConsumed);
                dueDate = _billingCalc.CalculateDueDate(issuedDate);
            }
            catch (Exception)
            {
                return (false, "Bill amount calculation failed. No bill was created.", null, null);
            }

            var bill = new Bill
            {
                CustomerId = customerId,
                BillingPeriod = billingPeriod,
                Amount = amount,
                IssuedDate = issuedDate,
                DueDate = dueDate,
                IsPaid = false
            };

            await CreateBillAsync(bill); // reuse the existing persistence method - no duplicate save logic

            return (true, "Bill generated successfully.", bill, usage.UnitsConsumed);
        }

        public async Task<bool> MarkBillAsPaidAsync(int billId)
        {
            var bill = await _context.Bills.FindAsync(billId);
            if (bill == null)
                return false;

            bill.IsPaid = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetBillUnpaidAsync(int billId)
        {
            var bill = await _context.Bills.FindAsync(billId);
            if (bill == null)
                return false;

            bill.IsPaid = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
