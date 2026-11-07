namespace NWSDB.API.DTOs
{
    public class SupportRequestDto
    {
        public int SupportRequestId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? StaffResponse { get; set; }
    }
}
