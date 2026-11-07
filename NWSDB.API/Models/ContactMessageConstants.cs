namespace NWSDB.API.Models
{
    /// <summary>
    /// The only statuses a Contact Message can have. A new message always starts as "New";
    /// Staff move it through "InProgress" to "Resolved" as it is handled.
    /// </summary>
    public static class ContactMessageStatuses
    {
        public const string New = "New";
        public const string InProgress = "InProgress";
        public const string Resolved = "Resolved";

        /// <summary>Returns the exact canonical value ("New" / "InProgress" / "Resolved") or false if invalid.</summary>
        public static bool TryNormalize(string? input, out string status)
        {
            var value = (input ?? string.Empty).Trim();

            foreach (var candidate in new[] { New, InProgress, Resolved })
            {
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    status = candidate;
                    return true;
                }
            }

            status = string.Empty;
            return false;
        }
    }
}
