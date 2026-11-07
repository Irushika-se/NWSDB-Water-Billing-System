using System.ComponentModel.DataAnnotations;

namespace NWSDB.API.DTOs
{
    // Register Account (Customer, self-service). CustomerId is assigned by the
    // server, not supplied by the customer.
    public class CustomerRegisterDto
    {
        // Nullable and validated by AuthController.RegisterCustomer (same rules and messages as
        // the Register form) so a bad request gets a clear message, not a generic 400.
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    // Customer Login -> include Authenticate User. Customers log in with their
    // registered Email Address, not their Customer ID.
    public class CustomerLoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    // Staff Login -> include Authenticate Staff
    public class StaffLoginDto
    {
        [Required] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    // Authenticate API Client (Third-Party Partner)
    public class PartnerAuthDto
    {
        [Required] public string PartnerId { get; set; } = string.Empty;
        [Required] public string ApiKey { get; set; } = string.Empty;
    }

    // QuickPay Lanka Login - a SEPARATE authentication system from Customer Login. Uses its
    // own QuickPay email/password; this is never checked against, or interchangeable with,
    // a customer's NWSDB password.
    public class QuickPayLoginDto
    {
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Password { get; set; } = string.Empty;
    }

    // Staff use case: Update Payment Status
    public class UpdatePaymentStatusDto
    {
        [Required] public bool IsPaid { get; set; }
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }

    // Response for QuickPay Login. Includes the authorized Customer ID(s) purely as a display
    // convenience for the QuickPay frontend (e.g. to know which dashboard to load) - it grants
    // nothing by itself. Every later request is authorized from the signed JWT, not this list.
    public class QuickPayAuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<string> AuthorizedCustomerIds { get; set; } = new();
    }
}
