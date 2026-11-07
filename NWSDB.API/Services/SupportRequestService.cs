using Microsoft.EntityFrameworkCore;
using NWSDB.API.Data;
using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public class SupportRequestService : ISupportRequestService
    {
        private readonly ApplicationDbContext _context;

        public SupportRequestService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SupportRequest>> GetByCustomerIdAsync(string customerId)
        {
            return await _context.SupportRequests
                .Where(r => r.CustomerId == customerId)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<SupportRequest> CreateAsync(string customerId, string subject, string message)
        {
            var request = new SupportRequest
            {
                CustomerId = customerId,
                Subject = subject,
                Message = message,
                Status = SupportRequestStatuses.New,
                CreatedDate = DateTime.Now
            };

            _context.SupportRequests.Add(request);
            await _context.SaveChangesAsync();

            return request;
        }

        public async Task<IEnumerable<SupportRequest>> GetAllAsync()
        {
            return await _context.SupportRequests
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<SupportRequest?> UpdateAsync(int supportRequestId, string status, string? staffResponse)
        {
            var request = await _context.SupportRequests.FindAsync(supportRequestId);
            if (request == null)
                return null;

            request.Status = status;
            request.StaffResponse = staffResponse;

            await _context.SaveChangesAsync();

            return request;
        }
    }
}
