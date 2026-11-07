namespace NWSDB.API.Models
{
    public class Partner
    {
        public string PartnerId { get; set; } = string.Empty;

        public string PartnerName { get; set; } = string.Empty;

        public string ContactEmail { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        // One of PartnerTypes ("Payment Gateway", "Mobile App", "Bank", "Other").
        public string PartnerType { get; set; } = string.Empty;

        // The rest are optional - a partner may not have these on hand at registration time.
        public string? BusinessAddress { get; set; }

        public string? BusinessRegistrationNo { get; set; }

        public string? Description { get; set; }

        // The raw API key is never stored. PartnerController.CreatePartner generates it,
        // shows it to Staff exactly once in the response, and stores only this SHA-256 hash.
        // AuthController.AuthenticatePartner hashes the key a partner sends and compares it
        // to this value (see Services/ApiKeyHasher.cs), so the key still works for real
        // authentication without the raw value ever living in the database.
        public string ApiKeyHash { get; set; } = string.Empty;
    }
}
