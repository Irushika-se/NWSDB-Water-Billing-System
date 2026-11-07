using System.ComponentModel.DataAnnotations;

namespace NWSDB.API.DTOs
{
    // Staff use case: Register Partner. PartnerId is never supplied by Staff - the server
    // generates it - and there is no ApiKey field here either: the server generates that too.
    public class PartnerRegisterDto
    {
        [Required] public string PartnerName { get; set; } = string.Empty;
        [Required] public string ContactEmail { get; set; } = string.Empty;
        [Required] public string PhoneNumber { get; set; } = string.Empty;
        [Required] public string PartnerType { get; set; } = string.Empty;
        public string? BusinessAddress { get; set; }
        public string? BusinessRegistrationNo { get; set; }
        public string? Description { get; set; }
    }

    // What GET /api/partner and GET /api/partner/{id} return. Deliberately has no key material
    // at all (not even the hash) - the raw API key is shown to Staff exactly once, in
    // PartnerRegisteredDto, right after registration, and never again after that.
    public class PartnerDto
    {
        public string PartnerId { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PartnerType { get; set; } = string.Empty;
        public string? BusinessAddress { get; set; }
        public string? BusinessRegistrationNo { get; set; }
        public string? Description { get; set; }
    }

    // What POST /api/partner returns: the only time the raw ApiKey is ever sent to a client.
    public class PartnerRegisteredDto
    {
        public string PartnerId { get; set; } = string.Empty;
        public string PartnerName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string PartnerType { get; set; } = string.Empty;
        public string? BusinessAddress { get; set; }
        public string? BusinessRegistrationNo { get; set; }
        public string? Description { get; set; }
        public string ApiKey { get; set; } = string.Empty;
    }
}
