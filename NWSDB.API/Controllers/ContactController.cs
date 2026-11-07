using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NWSDB.API.Models;
using NWSDB.API.DTOs;
using NWSDB.API.Services;

namespace NWSDB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        // Same pattern as PartnerController.EmailPattern / the frontend's EMAIL_REGEX.
        private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

        private const int MaxShortFieldLength = 200;
        private const int MaxMessageLength = 4000;

        private readonly IContactMessageService _contactMessageService;

        public ContactController(IContactMessageService contactMessageService)
        {
            _contactMessageService = contactMessageService;
        }

        // POST: api/contact  (Public use case: Submit Contact Message)
        // No [Authorize] - anyone (including a visitor who is not logged in) can submit
        // this. CreatedAt and Status are never taken from the client: CreatedAt is set to
        // the server's own clock and Status always starts as "New" (see
        // ContactMessageService.CreateAsync).
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateContactMessageDto dto)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            var email = (dto.Email ?? string.Empty).Trim();
            var subject = (dto.Subject ?? string.Empty).Trim();
            var message = (dto.Message ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name is required.");
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Email is required.");
            if (string.IsNullOrWhiteSpace(subject))
                return BadRequest("Subject is required.");
            if (string.IsNullOrWhiteSpace(message))
                return BadRequest("Message is required.");

            if (!EmailPattern.IsMatch(email))
                return BadRequest("Please enter a valid email address.");

            if (name.Length > MaxShortFieldLength)
                return BadRequest($"Name must be {MaxShortFieldLength} characters or fewer.");
            if (email.Length > MaxShortFieldLength)
                return BadRequest($"Email must be {MaxShortFieldLength} characters or fewer.");
            if (subject.Length > MaxShortFieldLength)
                return BadRequest($"Subject must be {MaxShortFieldLength} characters or fewer.");
            if (message.Length > MaxMessageLength)
                return BadRequest($"Message must be {MaxMessageLength} characters or fewer.");

            var contactMessage = await _contactMessageService.CreateAsync(name, email, subject, message);
            return Ok(ToDto(contactMessage));
        }

        // GET: api/contact  (Staff/Admin only: View Contact Messages)
        [HttpGet]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAll()
        {
            var messages = await _contactMessageService.GetAllAsync();
            return Ok(messages.Select(ToDto));
        }

        // GET: api/contact/5  (Staff/Admin only)
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetById(int id)
        {
            var message = await _contactMessageService.GetByIdAsync(id);
            if (message == null)
                return NotFound($"Contact message with ID {id} was not found.");

            return Ok(ToDto(message));
        }

        // PUT: api/contact/5/status  (Staff/Admin only: Update Contact Message Status)
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateContactMessageStatusDto dto)
        {
            if (!ContactMessageStatuses.TryNormalize(dto.Status, out var status))
                return BadRequest("Status must be one of: New, InProgress, Resolved.");

            var staffResponse = string.IsNullOrWhiteSpace(dto.StaffResponse) ? null : dto.StaffResponse.Trim();

            var message = await _contactMessageService.UpdateStatusAsync(id, status, staffResponse);
            if (message == null)
                return NotFound($"Contact message with ID {id} was not found.");

            return Ok(ToDto(message));
        }

        private static ContactMessageDto ToDto(ContactMessage message)
        {
            return new ContactMessageDto
            {
                Id = message.Id,
                Name = message.Name,
                Email = message.Email,
                Subject = message.Subject,
                Message = message.Message,
                CreatedAt = message.CreatedAt,
                Status = message.Status,
                StaffResponse = message.StaffResponse
            };
        }
    }
}
