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
    public class BillController : ControllerBase
    {
        private readonly IBillService _billService;

        public BillController(IBillService billService)
        {
            _billService = billService;
        }

        // GET: api/bill  (Staff: Manage Bill Information overview)
        [HttpGet]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAllBills()
        {
            var bills = await _billService.GetAllBillsAsync();
            return Ok(bills.Select(ToDto));
        }

        // GET: api/bill/1  (Staff, Partner: Retrieve Bill Details)
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Staff,Partner")]
        public async Task<IActionResult> GetBillById(int id)
        {
            var bill = await _billService.GetBillByIdAsync(id);
            if (bill == null)
                return NotFound($"Bill with ID {id} was not found.");

            return Ok(ToDto(bill));
        }

        // GET: api/bill/customer/C001  (Customer: View Bill Details, Partner: Retrieve Bill Details, Staff, QuickPayUser)
        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Customer,Partner,Staff,QuickPayUser")]
        public async Task<IActionResult> GetBillsByCustomer(string customerId)
        {
            if (!User.CanAccessCustomer(customerId))
                return Forbid();

            var bills = await _billService.GetBillsByCustomerIdAsync(customerId);
            if (!bills.Any())
                return NotFound($"No bills found for customer {customerId}.");

            return Ok(bills.Select(ToDto));
        }

        // GET: api/bill/customer/C001/current
        [HttpGet("customer/{customerId}/current")]
        [Authorize(Roles = "Customer,Partner,Staff,QuickPayUser")]
        public async Task<IActionResult> GetCurrentBill(string customerId)
        {
            if (!User.CanAccessCustomer(customerId))
                return Forbid();

            var bill = await _billService.GetCurrentBillAsync(customerId);
            if (bill == null)
                return NotFound($"No unpaid current bill found for customer {customerId}.");

            return Ok(ToDto(bill));
        }

        
        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GenerateBill([FromBody] GenerateBillDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, bill, unitsConsumed) =
                await _billService.GenerateBillAsync(dto.CustomerId, dto.BillingPeriod);

            if (!success || bill == null)
                return BadRequest(new { message });

            var result = ToDto(bill);
            result.UnitsConsumed = unitsConsumed;

            return CreatedAtAction(nameof(GetBillById), new { id = bill.BillId }, result);
        }

        private static BillDto ToDto(Bill bill)
        {
            return new BillDto
            {
                BillId = bill.BillId,
                CustomerId = bill.CustomerId,
                BillingPeriod = bill.BillingPeriod,
                Amount = bill.Amount,
                DueDate = bill.DueDate,
                IssuedDate = bill.IssuedDate,
                IsPaid = bill.IsPaid,
                Status = bill.IsPaid ? "Paid" : (bill.DueDate < DateTime.Now ? "Overdue" : "Unpaid")
            };
        }
    }
}
