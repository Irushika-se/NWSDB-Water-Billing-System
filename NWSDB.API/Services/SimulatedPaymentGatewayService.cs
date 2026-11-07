using System.Text.RegularExpressions;

namespace NWSDB.API.Services
{
    /// <summary>
    /// Simple SIMULATED payment gateway for the demo. It never contacts any real payment
    /// provider and never charges a real card. It validates the demo card fields and, when
    /// they pass, approves the payment with a generated reference (e.g. "SIM-3F9A1C7B2D40").
    ///
    /// Security: card details are only read here for validation. Nothing is stored or logged.
    /// </summary>
    public class SimulatedPaymentGatewayService : IPaymentGatewayService
    {
        private static readonly Regex ExpiryPattern =
            new(@"^(0[1-9]|1[0-2])\s*/\s*(\d{2})$", RegexOptions.Compiled);

        public Task<SimulatedChargeResult> ChargeAsync(SimulatedCardDetails card, decimal amount)
        {
            var error = Validate(card);
            if (error != null)
                return Task.FromResult(SimulatedChargeResult.Decline(error));

            var reference = "SIM-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
            return Task.FromResult(SimulatedChargeResult.Approve(reference));
        }

        // Returns a user-facing error message, or null when the demo card details are acceptable.
        private static string? Validate(SimulatedCardDetails card)
        {
            // Card number: digits only (spaces / hyphens allowed for readability), 13-19 digits.
            var cardNumber = (card.CardNumber ?? string.Empty).Replace(" ", "").Replace("-", "");
            if (cardNumber.Length == 0)
                return "Card number is required.";
            if (cardNumber.Length < 13 || cardNumber.Length > 19 || !cardNumber.All(char.IsAsciiDigit))
                return "Card number must contain 13 to 19 digits.";

            // Expiry date: MM/YY, and not already expired.
            var expiry = (card.ExpiryDate ?? string.Empty).Trim();
            if (expiry.Length == 0)
                return "Expiry date is required.";

            var match = ExpiryPattern.Match(expiry);
            if (!match.Success)
                return "Expiry date must be in MM/YY format.";

            var month = int.Parse(match.Groups[1].Value);
            var year = 2000 + int.Parse(match.Groups[2].Value);
            var now = DateTime.Now;
            if (year < now.Year || (year == now.Year && month < now.Month))
                return "This card has expired.";

            // CVV: 3 or 4 digits.
            var cvv = (card.Cvv ?? string.Empty).Trim();
            if (cvv.Length == 0)
                return "CVV is required.";
            if (cvv.Length < 3 || cvv.Length > 4 || !cvv.All(char.IsAsciiDigit))
                return "CVV must be 3 or 4 digits.";

            return null;
        }
    }
}
