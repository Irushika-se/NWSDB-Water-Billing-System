namespace NWSDB.API.DTOs
{
    
    public class SimulatePaymentDto
    {
        public string? CustomerId { get; set; }

        public int BillId { get; set; }

        // Exactly "Credit Card" or "Debit Card".
        public string? PaymentMethod { get; set; }

        public string? CardNumber { get; set; }

        public string? ExpiryDate { get; set; } // "MM/YY"

        public string? Cvv { get; set; }
    }
}
