using System.Text.RegularExpressions;

namespace NWSDB.API.Services
{
    /// <summary>
    /// Input rules shared by Customer Register and Update Profile. They mirror the browser
    /// form (NWSDB.Client/script.js) so the server enforces the same rules on its own.
    /// </summary>
    public static class InputRules
    {
        // Sri Lankan style numbers: 0 + 9 digits, or +94 + 9 digits.
        public static readonly Regex PhoneRegex = new(@"^(?:0|\+94)\d{9}$");

        public static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");

        // "077 123 4567" and "077-123-4567" are accepted and stored as "0771234567".
        public static string NormalizePhone(string? phone) =>
            Regex.Replace(phone ?? string.Empty, @"[\s-]", string.Empty);
    }
}
