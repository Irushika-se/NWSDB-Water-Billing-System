namespace NWSDB.API.Models
{
    /// <summary>
    /// The only two payment methods the (simulated) payment flow accepts. The value stored
    /// in Payment.PaymentMethod is always exactly one of these strings.
    /// </summary>
    public static class PaymentMethods
    {
        public const string CreditCard = "Credit Card";
        public const string DebitCard = "Debit Card";

        /// <summary>Returns the exact canonical value ("Credit Card" / "Debit Card") or false if invalid.</summary>
        public static bool TryNormalize(string? input, out string method)
        {
            var value = (input ?? string.Empty).Trim();

            if (string.Equals(value, CreditCard, StringComparison.OrdinalIgnoreCase))
            {
                method = CreditCard;
                return true;
            }

            if (string.Equals(value, DebitCard, StringComparison.OrdinalIgnoreCase))
            {
                method = DebitCard;
                return true;
            }

            method = string.Empty;
            return false;
        }
    }

    /// <summary>Who a payment was made through (stored in Payment.PaidByPartnerId).</summary>
    public static class PaymentSources
    {
        // Used when a customer pays through the NWSDB Customer Portal itself.
        public const string OnlinePortal = "NWSDB Online Portal";

        // Used when a payment is made through the QuickPay Lanka third-party portal, by a
        // QuickPayUser-role account. Set directly from the authenticated role in
        // PaymentController.SimulatePayment - never from anything the client sends.
        public const string QuickPayLanka = "QuickPay Lanka";
    }

    /// <summary>
    /// The status shown for a payment (Staff "View Payments" page). The simulated gateway
    /// only ever creates a Payment row once it has approved the charge (see
    /// PaymentService.ProcessSimulatedPaymentAsync) - a failed/declined attempt is never
    /// saved - so every Payment record that exists always reflects a completed payment.
    /// </summary>
    public static class PaymentStatuses
    {
        public const string Success = "SUCCESS";
    }
}
