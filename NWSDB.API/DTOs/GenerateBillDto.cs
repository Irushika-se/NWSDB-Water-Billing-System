using System.ComponentModel.DataAnnotations;

namespace NWSDB.API.DTOs
{
    /// <summary>
    /// Staff use case: Generate Bill. Staff supplies only who the bill is for and which
    /// billing period it covers - the Bill ID, water usage, Amount, Issued Date and Due
    /// Date are all worked out by the backend (BillService.GenerateBillAsync), never typed
    /// in here.
    /// </summary>
    public class GenerateBillDto
    {
        [Required]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        public string BillingPeriod { get; set; } = string.Empty;
    }
}
