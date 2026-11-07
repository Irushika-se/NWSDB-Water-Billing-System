namespace NWSDB.API.DTOs
{
    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public int BillId { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? PaidByPartnerId { get; set; }
        public string? GatewayReference { get; set; }

        // Every Payment record only ever exists once the simulated gateway approved it
        // (see PaymentService.ProcessSimulatedPaymentAsync), so this is always "SUCCESS".
        // Computed here, not stored on Payment - same pattern as BillDto.Status.
        public string Status { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}
