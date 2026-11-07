namespace NWSDB.API.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        public int BillId { get; set; }

        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.Now;

        public string PaymentMethod { get; set; } = string.Empty; // exactly "Credit Card" or "Debit Card"

        public string? PaidByPartnerId { get; set; } // "NWSDB Online Portal" = customer paid in the portal; a partner ID (e.g. "P001") = a Third Party Partner paid on the customer's behalf

       
        public string? GatewayReference { get; set; }
    }
}

