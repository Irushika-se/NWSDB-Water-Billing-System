using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    public interface ISupportRequestService
    {
        // Customer use case: View My Support Requests. Newest first.
        Task<IEnumerable<SupportRequest>> GetByCustomerIdAsync(string customerId);

        // Customer use case: Submit Support Request. Always starts as "New".
        Task<SupportRequest> CreateAsync(string customerId, string subject, string message);

        // Staff use case: View All Support Requests. Newest first.
        Task<IEnumerable<SupportRequest>> GetAllAsync();

        // Staff use case: Update Support Request Status / Respond to Support Request.
        Task<SupportRequest?> UpdateAsync(int supportRequestId, string status, string? staffResponse);
    }
}
