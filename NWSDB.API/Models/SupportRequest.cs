namespace NWSDB.API.Models
{
    public class SupportRequest
    {
        public int SupportRequestId { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // One of SupportRequestStatuses: "New", "In Progress" or "Resolved".
        public string Status { get; set; } = SupportRequestStatuses.New;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Set by Staff when they respond to the request (Staff use case: Respond to
        // Support Request). Null until a staff member adds a response.
        public string? StaffResponse { get; set; }
    }
}
