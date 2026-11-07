using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public interface IContactMessageService
    {
        // Public use case: Submit Contact Message. Always starts as "New";
        // CreatedAt is always set here, on the server.
        Task<ContactMessage> CreateAsync(string name, string email, string subject, string message);

        // Staff use case: View Contact Messages. Newest first.
        Task<IEnumerable<ContactMessage>> GetAllAsync();

        // Staff use case: View one Contact Message.
        Task<ContactMessage?> GetByIdAsync(int id);

        // Staff use case: Update Contact Message Status.
        Task<ContactMessage?> UpdateStatusAsync(int id, string status, string? staffResponse);
    }
}
