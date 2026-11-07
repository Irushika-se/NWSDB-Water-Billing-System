using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NWSDB.API.Auth;
using NWSDB.API.Models;
using NWSDB.API.DTOs;
using NWSDB.API.Services;

namespace NWSDB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupportRequestController : ControllerBase
    {
        private readonly ISupportRequestService _supportRequestService;

        public SupportRequestController(ISupportRequestService supportRequestService)
        {
            _supportRequestService = supportRequestService;
        }

        // GET: api/supportrequest  (Staff: View All Support Requests)
        [HttpGet]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAll()
        {
            var requests = await _supportRequestService.GetAllAsync();
            return Ok(requests.Select(ToDto));
        }

        // GET: api/supportrequest/customer/C001  (Customer: View My Support Requests)
        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Customer,Staff")]
        public async Task<IActionResult> GetByCustomer(string customerId)
        {
            if (!User.CanAccessCustomer(customerId))
                return Forbid();

            var requests = await _supportRequestService.GetByCustomerIdAsync(customerId);
            return Ok(requests.Select(ToDto));
        }

        // POST: api/supportrequest  (Customer: Submit Support Request)
        // The Customer ID comes ONLY from the authenticated JWT (User.GetUserId()), never
        // from the request body, so a customer can never file a request under another
        // customer's ID - same pattern as CustomerController.UpdateMyProfile.
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create([FromBody] CreateSupportRequestDto dto)
        {
            var myId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(myId))
                return Unauthorized();

            var subject = (dto.Subject ?? string.Empty).Trim();
            var message = (dto.Message ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(subject))
                return BadRequest("Subject is required.");
            if (string.IsNullOrWhiteSpace(message))
                return BadRequest("Message is required.");

            var request = await _supportRequestService.CreateAsync(myId, subject, message);
            return Ok(ToDto(request));
        }

        // PUT: api/supportrequest/5  (Staff: Update Support Request Status / Respond to Support Request)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSupportRequestDto dto)
        {
            if (!SupportRequestStatuses.TryNormalize(dto.Status, out var status))
                return BadRequest("Status must be one of: New, In Progress, Resolved.");

            var staffResponse = string.IsNullOrWhiteSpace(dto.StaffResponse) ? null : dto.StaffResponse.Trim();

            var request = await _supportRequestService.UpdateAsync(id, status, staffResponse);
            if (request == null)
                return NotFound($"Support request with ID {id} was not found.");

            return Ok(ToDto(request));
        }

        private static SupportRequestDto ToDto(SupportRequest request)
        {
            return new SupportRequestDto
            {
                SupportRequestId = request.SupportRequestId,
                CustomerId = request.CustomerId,
                Subject = request.Subject,
                Message = request.Message,
                Status = request.Status,
                CreatedDate = request.CreatedDate,
                StaffResponse = request.StaffResponse
            };
        }
    }
}
