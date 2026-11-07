using System.Security.Claims;

namespace NWSDB.API.Auth
{
    // Small helper used on every "customer/{id}"-style endpoint to enforce that a
    // Customer can only ever see/act on their OWN data. Staff and Partner keep the
    // broader access the use-case diagram gives them (Manage.../Retrieve... for any
    // customer), so this only tightens the Customer role. QuickPayUser sits between
    // the two: it may access exactly the Customer ID(s) its account was linked to at
    // login (see AuthController.QuickPayLogin) - never "any customer" like Partner,
    // and never decided by anything the browser sends on this request.
    public static class ClaimsPrincipalExtensions
    {
        public const string AuthorizedCustomerClaim = "quickpay_authorized_customer";

        public static bool CanAccessCustomer(this ClaimsPrincipal user, string customerId)
        {
            var role = user.FindFirstValue(ClaimTypes.Role);

            if (role == "Staff" || role == "Partner")
                return true;

            if (role == "Customer")
                return user.FindFirstValue(ClaimTypes.NameIdentifier) == customerId;

            if (role == "QuickPayUser")
                return user.FindAll(AuthorizedCustomerClaim).Any(c => c.Value == customerId);

            return false;
        }

        public static string? GetUserId(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier);

        public static string? GetRole(this ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.Role);
    }
}
