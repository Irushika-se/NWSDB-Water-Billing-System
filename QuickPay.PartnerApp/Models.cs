using System.Net;
using System.Text;
using System.Text.Json;

namespace QuickPay.PartnerApp;

// Settings for talking to the NWSDB main system (see appsettings.json -> "MainSystem").
// QuickPay no longer holds any credentials of its own: every authenticated call is made
// with the logged-in QuickPay account's own NWSDB JWT (see MainSystemClient.SendAsUserAsync),
// so all that is needed here is where NWSDB lives.
public class MainSystemOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5094";
}

// The NWSDB main system could not be reached.
public class MainSystemException : Exception
{
    public int StatusCode { get; }
    public MainSystemException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}

// One reply from the NWSDB API, with the body already parsed when it is JSON.
public record MainResponse(HttpStatusCode Status, string Text, JsonElement? Json)
{
    public bool Ok => (int)Status is >= 200 and <= 299;
    public bool NotFound => Status == HttpStatusCode.NotFound;
    public bool Unauthorized => Status == HttpStatusCode.Unauthorized || Status == HttpStatusCode.Forbidden;

    public static MainResponse From(HttpStatusCode status, string text)
    {
        JsonElement? json = null;
        try
        {
            using var doc = JsonDocument.Parse(text);
            json = doc.RootElement.Clone();
        }
        catch (JsonException) { /* plain-text body such as "Customer not found." */ }
        return new MainResponse(status, text, json);
    }

    // A human-readable error taken from whatever shape the NWSDB API used.
    public string Message
    {
        get
        {
            if (Json is { ValueKind: JsonValueKind.Object } obj)
            {
                foreach (var key in new[] { "message", "title" })
                    if (obj.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                        return v.GetString()!;
            }
            if (Json is { ValueKind: JsonValueKind.String } str) return str.GetString()!;
            if (Json == null && !string.IsNullOrWhiteSpace(Text)) return Text.Trim();
            return $"Request failed ({(int)Status})";
        }
    }
}

// QuickPay Login request - relayed as-is to NWSDB's POST /api/auth/quickpay/login. This is
// QuickPay Lanka's OWN separate authentication system: a QuickPay email/password, checked
// against the QuickPay account store, never against a customer's NWSDB password.
public record LoginRequest(string Email, string Password);

// Pay Bill request. Deliberately has NO CustomerId and NO Amount: the customer is whichever
// NWSDB Customer ID this QuickPay account is authorized for (see JwtPayload.GetAuthorizedCustomerIds),
// and the amount is always the bill's own amount, decided by NWSDB - never something the browser can send.
// ToString is overridden so a stray log line never includes the card fields.
public record PayRequest(int BillId, string PaymentMethod, string CardNumber, string ExpiryDate, string Cvv)
{
    public override string ToString() => $"PayRequest {{ BillId = {BillId}, PaymentMethod = {PaymentMethod} }}";
}

/// <summary>
/// Reads the NWSDB Customer ID(s) a QuickPay JWT is authorized for, straight out of the
/// token's payload - WITHOUT verifying its signature or expiry. This is only ever used to
/// build the "customer/{id}" segment of the NWSDB API URLs QuickPay relays to; it does not
/// grant access by itself. The same token is then forwarded as the Bearer credential on that
/// same request, so NWSDB independently validates the signature and re-checks (via its own
/// "quickpay_authorized_customer" claims, set when the QuickPay account logged in) that this
/// account actually owns whatever is being requested. A tampered or expired token is rejected
/// there regardless of what QuickPay read out of it here - this class is a routing
/// convenience, not a security boundary.
/// </summary>
public static class JwtPayload
{
    // Must match NWSDB.API.Auth.ClaimsPrincipalExtensions.AuthorizedCustomerClaim exactly.
    private const string AuthorizedCustomerClaim = "quickpay_authorized_customer";

    public static List<string> GetAuthorizedCustomerIds(string jwt)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length < 2) return new();

            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            using var doc = JsonDocument.Parse(payloadJson);

            if (!doc.RootElement.TryGetProperty(AuthorizedCustomerClaim, out var claim))
                return new();

            // A single claim serializes as one JSON string; two or more of the same claim
            // type serialize as a JSON array - handle both.
            if (claim.ValueKind == JsonValueKind.Array)
                return claim.EnumerateArray()
                    .Where(e => e.ValueKind == JsonValueKind.String)
                    .Select(e => e.GetString()!)
                    .ToList();

            if (claim.ValueKind == JsonValueKind.String)
                return new List<string> { claim.GetString()! };

            return new();
        }
        catch
        {
            return new();
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
