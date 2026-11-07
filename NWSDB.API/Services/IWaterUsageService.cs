using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public interface IWaterUsageService
    {
        Task<IEnumerable<WaterUsage>> GetAllUsageAsync();
        Task<IEnumerable<WaterUsage>> GetUsageByCustomerIdAsync(string customerId);
        Task<WaterUsage?> GetLatestUsageAsync(string customerId);

        // Used by BillService.GenerateBillAsync to reuse the meter reading already recorded
        // for this customer/billing period (Record Meter Reading) instead of asking Staff to
        // enter the usage again on the Generate Bill page.
        Task<WaterUsage?> GetUsageForPeriodAsync(string customerId, string billingPeriod);

        // Staff use case: Generate Bill -> "Load Billing Periods". Computed with a single
        // NOT EXISTS query directly against WaterUsages + Bills (no dependency on
        // IBillService), so this never hard-codes periods, never duplicates the list in the
        // frontend, and can't be taken down by an unrelated failure elsewhere in the Bill
        // read path. Returns exactly the customer's recorded meter readings that do not yet
        // have a Bill for the same CustomerId + BillingPeriod.
        Task<IEnumerable<WaterUsage>> GetUnbilledUsageAsync(string customerId);

        Task<WaterUsage> CreateUsageAsync(WaterUsage usage);

        // Used by WaterUsageController.CreateUsage (Staff use case: Record Meter Reading).
        // Validates the customer exists, units consumed isn't negative and no reading already
        // exists for this customer + billing period, before creating the record - mirrors
        // BillService.GenerateBillAsync's validate-then-create pattern.
        Task<(bool Success, string Message, WaterUsage? Usage)> RecordUsageAsync(
            string customerId, string billingPeriod, decimal unitsConsumed, DateTime readingDate);
    }
}
