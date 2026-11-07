using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWSDB.API.Auth;
using NWSDB.API.Data;
using NWSDB.API.Models;
using NWSDB.API.DTOs;
using NWSDB.API.Services;
using System.Security.Claims;

namespace NWSDB.API.Controllers
{
    
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController : ControllerBase
    {
        private readonly DirectoryStore _store;
        private readonly ApplicationDbContext _db;
        private readonly JwtTokenService _jwt;

        public AuthController(DirectoryStore store, ApplicationDbContext db, JwtTokenService jwt)
        {
            _store = store;
            _db = db;
            _jwt = jwt;
        }

        // POST: api/auth/customer/register  (Register Account)
        [HttpPost("customer/register")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CustomerRegisterDto dto)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            var address = (dto.Address ?? string.Empty).Trim();
            var phone = InputRules.NormalizePhone(dto.Phone);
            var email = (dto.Email ?? string.Empty).Trim();
            var password = dto.Password ?? string.Empty;

            // Server-side validation: the same rules and messages as the Register form in the
            // browser, so a request sent without the form is checked too.
            if (name.Length == 0)
                return RegisterError("Name is required.");
            if (address.Length == 0)
                return RegisterError("Address is required.");
            if (phone.Length == 0)
                return RegisterError("Phone number is required.");
            if (!InputRules.PhoneRegex.IsMatch(phone))
                return RegisterError("Please enter a valid phone number (e.g. 0771234567).");
            if (email.Length == 0)
                return RegisterError("Email address is required.");
            if (!InputRules.EmailRegex.IsMatch(email))
                return RegisterError("Please enter a valid email address.");
            if (password.Length == 0)
                return RegisterError("Password is required.");
            if (password.Length < 4)
                return RegisterError("Password must be at least 4 characters long.");

            
            var emailLower = email.ToLower();
            var emailTaken = await _db.Customers.AnyAsync(c => c.Email.Trim().ToLower() == emailLower);
            if (emailTaken)
                return RegisterError("An account with this email already exists.");

            var customer = new Customer
            {
                CustomerId = await GenerateNextCustomerIdAsync(),
                Name = name,
                Address = address,
                Phone = phone,
                Email = email,
                Password = password
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            var token = _jwt.GenerateToken(customer.CustomerId, "Customer", customer.Name);
            return Ok(new AuthResponseDto { Token = token, Role = "Customer", SubjectId = customer.CustomerId, DisplayName = customer.Name });
        }

        // Validation errors are returned as { "message": "..." } so the client shows the text.
        private IActionResult RegisterError(string message) => BadRequest(new { message });

        // Assigns the next sequential Customer ID (C001, C002, ...) based on the
        // highest existing numeric suffix among the customers stored in the database.
        private async Task<string> GenerateNextCustomerIdAsync()
        {
            var maxNumber = 0;
            var ids = await _db.Customers.Select(c => c.CustomerId).ToListAsync();
            foreach (var id in ids)
            {
                if (id.Length > 1 && id[0] == 'C' &&
                    int.TryParse(id.Substring(1), out var num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }
            return "C" + (maxNumber + 1).ToString("D3");
        }

        // POST: api/auth/customer/login  (Customer Login -> include Authenticate User)
        [HttpPost("customer/login")]
        public async Task<IActionResult> CustomerLogin([FromBody] CustomerLoginDto dto)
        {
            var emailLower = (dto.Email ?? string.Empty).Trim().ToLower();
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Email.Trim().ToLower() == emailLower);
            if (customer == null || customer.Password != dto.Password)
                return Unauthorized(new { message = "Invalid email or password." });

            var token = _jwt.GenerateToken(customer.CustomerId, "Customer", customer.Name);
            return Ok(new AuthResponseDto { Token = token, Role = "Customer", SubjectId = customer.CustomerId, DisplayName = customer.Name });
        }

        // POST: api/auth/staff/login  (Staff Login -> include Authenticate Staff)
        [HttpPost("staff/login")]
        public IActionResult StaffLogin([FromBody] StaffLoginDto dto)
        {
            var email = (dto.Email ?? string.Empty).Trim();
            var staff = _store.StaffMembers.FirstOrDefault(s =>
                string.Equals(s.Email, email, StringComparison.OrdinalIgnoreCase));
            if (staff == null || staff.Password != dto.Password)
                return Unauthorized(new { message = "Invalid staff email or password." });

            var token = _jwt.GenerateToken(staff.StaffId, "Staff", staff.Name);
            return Ok(new AuthResponseDto { Token = token, Role = "Staff", SubjectId = staff.StaffId, DisplayName = staff.Name });
        }

        // POST: api/auth/partner/authenticate  (Authenticate API Client)
        [HttpPost("partner/authenticate")]
        public IActionResult AuthenticatePartner([FromBody] PartnerAuthDto dto)
        {
            var partner = _store.Partners.FirstOrDefault(p => p.PartnerId == dto.PartnerId);
            if (partner == null || !ApiKeyHasher.Verify(dto.ApiKey, partner.ApiKeyHash))
                return Unauthorized(new { message = "Invalid Partner ID or API key." });

            var token = _jwt.GenerateToken(partner.PartnerId, "Partner", partner.PartnerName);
            return Ok(new AuthResponseDto { Token = token, Role = "Partner", SubjectId = partner.PartnerId, DisplayName = partner.PartnerName });
        }

        
        [HttpPost("quickpay/login")]
        public IActionResult QuickPayLogin([FromBody] QuickPayLoginDto dto)
        {
            var account = _store.QuickPayAccounts.FirstOrDefault(a =>
                string.Equals(a.Email, dto.Email, StringComparison.OrdinalIgnoreCase));
            if (account == null || account.Password != dto.Password)
                return Unauthorized(new { message = "Invalid QuickPay email or password." });

            var customerClaims = account.AuthorizedCustomerIds
                .Select(id => new Claim(ClaimsPrincipalExtensions.AuthorizedCustomerClaim, id));

            var token = _jwt.GenerateToken(account.QuickPayAccountId, "QuickPayUser", account.DisplayName, customerClaims);

            return Ok(new QuickPayAuthResponseDto
            {
                Token = token,
                Role = "QuickPayUser",
                SubjectId = account.QuickPayAccountId,
                DisplayName = account.DisplayName,
                AuthorizedCustomerIds = account.AuthorizedCustomerIds
            });
        }
    }
}
