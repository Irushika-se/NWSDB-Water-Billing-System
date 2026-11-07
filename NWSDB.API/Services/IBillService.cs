using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public interface IBillService
    {
        Task<IEnumerable<Bill>> GetAllBillsAsync();
        Task<Bill?> GetBillByIdAsync(int billId);
        Task<IEnumerable<Bill>> GetBillsByCustomerIdAsync(string customerId);
        Task<Bill?> GetCurrentBillAsync(string customerId);
        Task<Bill> CreateBillAsync(Bill bill);

        // Staff use case: Generate Bill. Validates the customer and the billing period,
        // reuses the already-recorded water usage for that period (Record Meter Reading),
        // calculates the amount/due date via IBillingCalculationService (the existing
        // NWSDB tariff), and creates the Bill. Never accepts a Staff-supplied amount.
        // No Bill is created if any validation or the calculation itself fails.
        Task<(bool Success, string Message, Bill? Bill, decimal? UnitsConsumed)> GenerateBillAsync(
            string customerId, string billingPeriod);

        Task<bool> MarkBillAsPaidAsync(int billId);
        Task<bool> SetBillUnpaidAsync(int billId);
    }
}
