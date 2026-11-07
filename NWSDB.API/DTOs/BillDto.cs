
namespace NWSDB.API.DTOs
{
    public class BillDto
    {
        public int BillId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string BillingPeriod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime IssuedDate { get; set; }
        public bool IsPaid { get; set; }
        public string Status { get; set; } = string.Empty;

        // Only populated on the Generate Bill response (POST /api/bill), so the Staff
        // Portal's "Bill Generated Successfully" summary can show the water usage that
        // the amount was calculated from. Null everywhere else - it is not stored on the
        // Bill itself, it's looked up from the existing water usage record on demand.
        public decimal? UnitsConsumed { get; set; }
    }
}



