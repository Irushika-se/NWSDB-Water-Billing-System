namespace NWSDB.API.DTOs
{
    // Staff use case: Update Support Request Status / Respond to Support Request.
    public class UpdateSupportRequestDto
    {
        public string? Status { get; set; }
        public string? StaffResponse { get; set; }
    }
}
