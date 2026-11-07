using System.ComponentModel.DataAnnotations;

namespace NWSDB.API.DTOs
{
    public class CreateWaterUsageDto
    {
        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        public string BillingPeriod { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Units consumed must be a positive value.")]
        public decimal UnitsConsumed { get; set; }

        [Required]
        public DateTime ReadingDate { get; set; }
    }
}
