namespace NWSDB.API.Models
{
    public class ContactMessage
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        // Always set by the server (never trusted from the client) - see
        // ContactController.Create / ContactMessageService.CreateAsync.
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // One of ContactMessageStatuses: "New", "InProgress" or "Resolved".
        // Always starts as "New" - only Staff can change it (PUT /api/contact/{id}/status).
        public string Status { get; set; } = ContactMessageStatuses.New;

        // Set by Staff when they respond to the message (Staff use case: Respond to
        // Contact Message). Null until a staff member adds a response.
        public string? StaffResponse { get; set; }
    }
}
