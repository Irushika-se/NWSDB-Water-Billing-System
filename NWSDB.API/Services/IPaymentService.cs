using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public interface IPaymentService
    {
        Task<IEnumerable<Payment>> GetAllPaymentsAsync();
        Task<Payment?> GetPaymentByIdAsync(int paymentId);
        Task<IEnumerable<Payment>> GetPaymentsByCustomerIdAsync(string customerId);

        // Process Payment -> Generate Payment Confirmation (SIMULATED, demo only).
        // Validates the bill and the demo card details, "approves" the payment through the
        // simulated gateway, saves the Payment record and marks the bill as paid - all or
        // nothing. The full card number and CVV are never stored.
        //   paymentMethod : "Credit Card" or "Debit Card"
        //   paidBy        : "NWSDB Online Portal" for the customer portal, or the partner's ID
        Task<(bool Success, string Message, Payment? Payment)> ProcessSimulatedPaymentAsync(
            string customerId, int billId, string? paymentMethod, SimulatedCardDetails card, string paidBy);
    }
}
