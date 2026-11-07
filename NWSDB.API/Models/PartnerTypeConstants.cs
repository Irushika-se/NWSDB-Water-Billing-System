namespace NWSDB.API.Models
{
    /// <summary>
    /// The category a registered Partner falls under, chosen by Staff at registration
    /// (Register Partner use case).
    /// </summary>
    public static class PartnerTypes
    {
        public const string PaymentGateway = "Payment Gateway";
        public const string MobileApp = "Mobile App";
        public const string Bank = "Bank";
        public const string Other = "Other";

        /// <summary>Returns the exact canonical value or false if invalid.</summary>
        public static bool TryNormalize(string? input, out string partnerType)
        {
            var value = (input ?? string.Empty).Trim();

            foreach (var candidate in new[] { PaymentGateway, MobileApp, Bank, Other })
            {
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    partnerType = candidate;
                    return true;
                }
            }

            partnerType = string.Empty;
            return false;
        }
    }
}
