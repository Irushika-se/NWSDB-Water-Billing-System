using Microsoft.EntityFrameworkCore;
using NWSDB.API.Data;
using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public class ContactMessageService : IContactMessageService
    {
        private readonly ApplicationDbContext _context;

        public ContactMessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ContactMessage> CreateAsync(string name, string email, string subject, string message)
        {
            var contactMessage = new ContactMessage
            {
                Name = name,
                Email = email,
                Subject = subject,
                Message = message,
                Status = ContactMessageStatuses.New,
                CreatedAt = DateTime.Now
            };

            _context.ContactMessages.Add(contactMessage);
            await _context.SaveChangesAsync();

            return contactMessage;
        }

        public async Task<IEnumerable<ContactMessage>> GetAllAsync()
        {
            return await _context.ContactMessages
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<ContactMessage?> GetByIdAsync(int id)
        {
            return await _context.ContactMessages.FindAsync(id);
        }

        public async Task<ContactMessage?> UpdateStatusAsync(int id, string status, string? staffResponse)
        {
            var contactMessage = await _context.ContactMessages.FindAsync(id);
            if (contactMessage == null)
                return null;

            contactMessage.Status = status;
            contactMessage.StaffResponse = staffResponse;
            await _context.SaveChangesAsync();

            return contactMessage;
        }
    }
}
