using Microsoft.EntityFrameworkCore;
using NWSDB.API.Data;
using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IBillService _billService;
        private readonly IPaymentGatewayService _gateway;

        public PaymentService(ApplicationDbContext context, IBillService billService, IPaymentGatewayService gateway)
        {
            _context = context;
            _billService = billService;
            _gateway = gateway;
        }

        // "2026-07" -> "July 2026" (falls back to the raw value if it isn't YYYY-MM).
        private static string FormatPeriod(string period) =>
            DateTime.TryParseExact(period + "-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d)
                ? d.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture)
                : period;

        public async Task<IEnumerable<Payment>> GetAllPaymentsAsync()
        {
            return await _context.Payments
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
        {
            return await _context.Payments.FindAsync(paymentId);
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByCustomerIdAsync(string customerId)
        {
            return await _context.Payments
                .Where(p => p.CustomerId == customerId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public async Task<(bool Success, string Message, Payment? Payment)> ProcessSimulatedPaymentAsync(
            string customerId, int billId, string? paymentMethod, SimulatedCardDetails card, string paidBy)
        {
            // 1. The bill must exist, belong to this customer and still be unpaid.
            var bill = await _billService.GetBillByIdAsync(billId);

            if (bill == null)
                return (false, $"Bill with ID {billId} was not found.", null);

            if (bill.CustomerId != customerId)
                return (false, "This bill does not belong to the specified customer.", null);

            if (bill.IsPaid)
                return (false, "This bill has already been paid.", null);

            
            var oldest = await _billService.GetCurrentBillAsync(customerId);
            if (oldest != null && oldest.BillId != bill.BillId)
                return (false,
                    $"Please pay your oldest outstanding bill first (Bill ID {oldest.BillId}, {FormatPeriod(oldest.BillingPeriod)}) before paying this one.",
                    null);

            // 2. The payment method must be exactly "Credit Card" or "Debit Card".
            if (!PaymentMethods.TryNormalize(paymentMethod, out var method))
                return (false, "Please select a payment method: Credit Card or Debit Card.", null);

            // 3. Simulated gateway: validates the demo card details and approves the payment.
            //    The amount charged is always the bill's own amount, never a client-supplied one.
            var charge = await _gateway.ChargeAsync(card, bill.Amount);
            if (!charge.Approved)
                return (false, charge.Message, null);

            
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var payment = new Payment
            {
                CustomerId = bill.CustomerId,
                BillId = bill.BillId,
                AmountPaid = bill.Amount,
                PaymentDate = DateTime.Now,
                PaymentMethod = method,
                PaidByPartnerId = paidBy,
                GatewayReference = charge.Reference
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            await _billService.MarkBillAsPaidAsync(bill.BillId);

            await transaction.CommitAsync();

            return (true, "Payment successful (simulated payment for demonstration).", payment);
        }
    }
}
