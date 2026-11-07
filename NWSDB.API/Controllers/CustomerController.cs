using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWSDB.API.Auth;
using NWSDB.API.Data;
using NWSDB.API.Models;
using NWSDB.API.DTOs;
using NWSDB.API.Services;

namespace NWSDB.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public CustomerController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: api/customer  (Staff: overview of all connections)
        [HttpGet]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await _db.Customers.AsNoTracking().OrderBy(c => c.CustomerId).ToListAsync();
            return Ok(customers.Select(ToDto));
        }

        // GET: api/customer/C001
        // Customer viewing own account, Partner "Retrieve Customer Information",
        // Staff "Manage Customer Details", QuickPayUser viewing an authorized customer.
        [HttpGet("{id}")]
        [Authorize(Roles = "Customer,Partner,Staff,QuickPayUser")]
        public async Task<IActionResult> GetCustomer(string id)
        {
            if (!User.CanAccessCustomer(id))
                return Forbid();

            var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == id);

            if (customer == null)
                return NotFound("Customer not found.");

            return Ok(ToDto(customer));
        }

        // POST: api/customer  (Staff use case: Register Connection)
        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> CreateCustomer([FromBody] Customer newCustomer)
        {
            if (string.IsNullOrWhiteSpace(newCustomer.CustomerId) ||
                string.IsNullOrWhiteSpace(newCustomer.Name) ||
                string.IsNullOrWhiteSpace(newCustomer.Address) ||
                string.IsNullOrWhiteSpace(newCustomer.Phone))
            {
                return BadRequest("CustomerId, Name, Address and Phone are all required.");
            }

            if (await _db.Customers.AnyAsync(c => c.CustomerId == newCustomer.CustomerId))
            {
                return BadRequest($"Customer ID {newCustomer.CustomerId} already exists.");
            }

            // Staff-created connections get a default password the customer can change later
            // by re-registering login credentials; kept simple for this coursework demo.
            if (string.IsNullOrWhiteSpace(newCustomer.Password))
                newCustomer.Password = "changeme123";

            _db.Customers.Add(newCustomer);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCustomer), new { id = newCustomer.CustomerId }, ToDto(newCustomer));
        }

        // PUT: api/customer/C001  (Staff use case: Manage Customer Details)
        [HttpPut("{id}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> UpdateCustomer(string id, [FromBody] Customer updated)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == id);
            if (customer == null)
                return NotFound("Customer not found.");

            if (!string.IsNullOrWhiteSpace(updated.Name)) customer.Name = updated.Name;
            if (!string.IsNullOrWhiteSpace(updated.Address)) customer.Address = updated.Address;
            if (!string.IsNullOrWhiteSpace(updated.Phone)) customer.Phone = updated.Phone;

            await _db.SaveChangesAsync();
            return Ok(ToDto(customer));
        }

        // PUT: api/customer/me  (Customer use case: Update Customer Profile)
        // The Customer ID comes ONLY from the authenticated JWT (User.GetUserId()),
        // never from the request body or URL, so a customer can never target another
        // customer's record - even if the request is edited by hand. CustomerId and
        // Email are not part of UpdateProfileDto, so they cannot be changed here.
        [HttpPut("me")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto updated)
        {
            var myId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(myId))
                return Unauthorized();

            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.CustomerId == myId);
            if (customer == null)
                return NotFound("Customer not found.");

            var name = (updated.Name ?? string.Empty).Trim();
            var address = (updated.Address ?? string.Empty).Trim();
            // "077 123 4567" / "077-123-4567" are accepted and saved as "0771234567".
            var phone = InputRules.NormalizePhone(updated.Phone);

            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Name is required.");
            if (string.IsNullOrWhiteSpace(address))
                return BadRequest("Address is required.");
            if (!PhoneRegex.IsMatch(phone))
                return BadRequest("Please enter a valid phone number (e.g. 0771234567).");

            customer.Name = name;
            customer.Address = address;
            customer.Phone = phone;

            await _db.SaveChangesAsync();
            return Ok(ToDto(customer));
        }

        // Sri Lankan style numbers: 0 + 9 digits, or +94 + 9 digits. Mirrors the
        // frontend's PHONE_REGEX so the server never trusts client-side validation alone.
        private static readonly System.Text.RegularExpressions.Regex PhoneRegex =
            new(@"^(?:0|\+94)\d{9}$");

        // Never return the Customer entity itself: it carries the Password.
        private static CustomerDto ToDto(Customer c) => new()
        {
            CustomerId = c.CustomerId,
            Name = c.Name,
            Address = c.Address,
            Phone = c.Phone,
            Email = c.Email
        };
    }
}
