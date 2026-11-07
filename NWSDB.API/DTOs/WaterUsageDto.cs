namespace NWSDB.API.DTOs
{
    public class WaterUsageDto
    {
        public int UsageId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public decimal UnitsConsumed { get; set; }
        public DateTime ReadingDate { get; set; }

        // The bill this usage record was billed under, if any. Looked up from the Bill
        // table by matching CustomerId + BillingPeriod (there is no bill yet for a
        // reading that hasn't been billed). Never set from the client - always resolved
        // server-side in WaterUsageController so it can't be hardcoded or spoofed.
        public int? BillId { get; set; }
    }
}
