namespace NWSDB.API.DTOs
{
    // What the API returns for a customer. Deliberately has NO Password field, so login
    // credentials are never sent to Customers, Staff screens or Third-Party Partners.
    public class CustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // What a Customer may submit to update their OWN profile (Update Customer Profile
    // use case). Deliberately has NO CustomerId and NO Email: CustomerId is the
    // primary key and Email is the login identity, so both stay read-only and are
    // taken from the authenticated token / existing record, never from this body.
    public class UpdateProfileDto
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
