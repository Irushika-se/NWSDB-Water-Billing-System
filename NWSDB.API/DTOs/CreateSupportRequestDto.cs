namespace NWSDB.API.DTOs
{
    // The Customer ID is never taken from this DTO - the controller always uses the
    // authenticated JWT's own customer ID (see CustomerController.UpdateMyProfile for the
    // same pattern), so a customer can never file a support request under another
    // customer's ID.
    public class CreateSupportRequestDto
    {
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }
}
