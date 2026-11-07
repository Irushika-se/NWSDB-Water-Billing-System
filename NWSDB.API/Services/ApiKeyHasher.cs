using System.Security.Cryptography;
using System.Text;

namespace NWSDB.API.Services
{
    /// <summary>
    /// Hashes partner API keys for storage, the same way a password would be hashed.
    /// The raw key is only ever seen once, at the moment PartnerController generates it.
    /// </summary>
    public static class ApiKeyHasher
    {
        // Hex-encoded SHA-256 of the raw key. A hash can't be turned back into the key,
        // so storing this instead of the key itself is safe even if the database leaks.
        public static string Hash(string rawKey)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey ?? string.Empty));
            return Convert.ToHexString(bytes); // upper-case hex, e.g. "3F2A9B..."
        }

        // Verifies a raw key against a previously-stored hash. Uses a fixed-time comparison
        // so responses don't leak timing information about how much of the key matched.
        public static bool Verify(string rawKey, string storedHash)
        {
            if (string.IsNullOrEmpty(rawKey) || string.IsNullOrEmpty(storedHash))
                return false;

            var candidate = Encoding.UTF8.GetBytes(Hash(rawKey));
            var expected = Encoding.UTF8.GetBytes(storedHash);

            if (candidate.Length != expected.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(candidate, expected);
        }

        // A new, high-entropy raw API key for a newly-registered partner (192 random bits).
        public static string GenerateRawKey()
        {
            return "pk_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        }
    }
}
