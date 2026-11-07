using System.ComponentModel.DataAnnotations;

namespace NWSDB.API.Models
{
    public class WaterUsage
    {
        [Key]
        public int UsageId { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        public string BillingPeriod { get; set; } = string.Empty; // e.g. "2026-08"

        public decimal UnitsConsumed { get; set; } // cubic meters

        public DateTime ReadingDate { get; set; }
    }
}
