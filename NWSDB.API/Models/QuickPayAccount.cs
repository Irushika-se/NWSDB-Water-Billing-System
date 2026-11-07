namespace NWSDB.API.Models
{
    /// <summary>
    /// A QuickPay Lanka (third-party portal) login account - completely separate from any
    /// Customer's own NWSDB account. Authenticated independently, with its own email/password
    /// (never the customer's NWSDB password, never checked against the Customer table), and
    /// scoped to only the NWSDB Customer ID(s) it has been explicitly linked to here - never
    /// "any customer", the way the broader Partner role is.
    /// </summary>
    public class QuickPayAccount
    {
        public string QuickPayAccountId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        // The NWSDB Customer ID(s) this QuickPay account may act for. Decided here, on the
        // server, when the account is set up - never supplied by the browser at login or
        // request time. Baked into the JWT at login (see AuthController.QuickPayLogin) so
        // every later request re-checks it from the signed token, not from this list again.
        public List<string> AuthorizedCustomerIds { get; set; } = new();
    }
}
