namespace NWSDB.API.Models
{
    public class Staff
    {
        public string StaffId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // Plain text for this coursework demo only — hash in a real system.
        public string Password { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}
