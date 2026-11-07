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
    [Authorize(Roles = "Staff")]
    public class PartnerController : ControllerBase
    {
        private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);

        private readonly DirectoryStore _store;

        public PartnerController(DirectoryStore store)
        {
            _store = store;
        }

        // GET: api/partner  (Staff: view registered partners)
        // Never returns key material - not the raw key (never stored) and not even the hash.
        [HttpGet]
        public IActionResult GetPartners()
        {
            return Ok(_store.Partners.Select(ToDto));
        }

        // GET: api/partner/P001
        [HttpGet("{id}")]
        public IActionResult GetPartner(string id)
        {
            var partner = _store.Partners.FirstOrDefault(p => p.PartnerId == id);

            if (partner == null)
                return NotFound("Partner not found.");

            return Ok(ToDto(partner));
        }

        // POST: api/partner  (Staff use case: Register Partner)
        // PartnerId is generated here, never supplied by Staff. A fresh, high-entropy API key
        // is generated and returned exactly once in the response - only its SHA-256 hash is
        // kept (ApiKeyHasher), so the raw key can never be read back from the server again.
        // The partner uses this same raw key with POST /api/auth/partner/authenticate, which
        // verifies it against the stored hash (see AuthController.AuthenticatePartner).
        [HttpPost]
        public IActionResult CreatePartner([FromBody] PartnerRegisterDto dto)
        {
            var name = (dto.PartnerName ?? string.Empty).Trim();
            var email = (dto.ContactEmail ?? string.Empty).Trim();
            var phone = (dto.PhoneNumber ?? string.Empty).Trim();
            var address = string.IsNullOrWhiteSpace(dto.BusinessAddress) ? null : dto.BusinessAddress.Trim();
            var regNo = string.IsNullOrWhiteSpace(dto.BusinessRegistrationNo) ? null : dto.BusinessRegistrationNo.Trim();
            var description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Partner Name is required.");

            if (string.IsNullOrWhiteSpace(email))
                return BadRequest("Contact Email is required.");

            if (!EmailPattern.IsMatch(email))
                return BadRequest("Please enter a valid contact email address.");

            if (string.IsNullOrWhiteSpace(phone))
                return BadRequest("Phone Number is required.");

            if (!PartnerTypes.TryNormalize(dto.PartnerType, out var partnerType))
                return BadRequest("Partner Type must be one of: Payment Gateway, Mobile App, Bank, Other.");

            // Duplicate partner handling: one partner per contact email.
            if (_store.Partners.Any(p => string.Equals(p.ContactEmail, email, StringComparison.OrdinalIgnoreCase)))
                return BadRequest($"A partner with contact email {email} is already registered.");

            var partner = new Partner
            {
                PartnerId = GenerateNextPartnerId(),
                PartnerName = name,
                ContactEmail = email,
                PhoneNumber = phone,
                PartnerType = partnerType,
                BusinessAddress = address,
                BusinessRegistrationNo = regNo,
                Description = description
            };

            var rawApiKey = ApiKeyHasher.GenerateRawKey();
            partner.ApiKeyHash = ApiKeyHasher.Hash(rawApiKey);

            _store.Partners.Add(partner);

            return CreatedAtAction(nameof(GetPartner), new { id = partner.PartnerId }, new PartnerRegisteredDto
            {
                PartnerId = partner.PartnerId,
                PartnerName = partner.PartnerName,
                ContactEmail = partner.ContactEmail,
                PhoneNumber = partner.PhoneNumber,
                PartnerType = partner.PartnerType,
                BusinessAddress = partner.BusinessAddress,
                BusinessRegistrationNo = partner.BusinessRegistrationNo,
                Description = partner.Description,
                ApiKey = rawApiKey
            });
        }

        // Assigns the next sequential Partner ID (P001, P002, ...) based on the highest
        // existing numeric suffix among current partners - same pattern as customer/staff IDs.
        private string GenerateNextPartnerId()
        {
            var maxNumber = 0;
            foreach (var p in _store.Partners)
            {
                if (p.PartnerId.Length > 1 && p.PartnerId[0] == 'P' &&
                    int.TryParse(p.PartnerId.Substring(1), out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }
            return "P" + (maxNumber + 1).ToString("D3");
        }

        private static PartnerDto ToDto(Partner p) => new()
        {
            PartnerId = p.PartnerId,
            PartnerName = p.PartnerName,
            ContactEmail = p.ContactEmail,
            PhoneNumber = p.PhoneNumber,
            PartnerType = p.PartnerType,
            BusinessAddress = p.BusinessAddress,
            BusinessRegistrationNo = p.BusinessRegistrationNo,
            Description = p.Description
        };
    }
}
