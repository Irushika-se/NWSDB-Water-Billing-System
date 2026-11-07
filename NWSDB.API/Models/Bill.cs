namespace NWSDB.API.Models
{
    public class Bill
    {
        public int BillId { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        public string BillingPeriod { get; set; } = string.Empty; // e.g. "2026-08"

        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        public DateTime IssuedDate { get; set; }

        public bool IsPaid { get; set; } = false;
    }
}
