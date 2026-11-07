namespace NWSDB.API.DTOs
{
    // Public: Submit Contact Message (POST /api/contact). CreatedAt and Status are never
    // part of this DTO - both are always set by the server (see ContactController.Create).
    public class CreateContactMessageDto
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Subject { get; set; }
        public string? Message { get; set; }
    }
}
