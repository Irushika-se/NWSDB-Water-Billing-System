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
    public class WaterUsageController : ControllerBase
    {
        private readonly IWaterUsageService _usageService;
        private readonly IBillService _billService;

        public WaterUsageController(IWaterUsageService usageService, IBillService billService)
        {
            _usageService = usageService;
            _billService = billService;
        }

        // GET: api/waterusage  (Staff overview)
        [HttpGet]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAllUsage()
        {
            var usage = await _usageService.GetAllUsageAsync();
            var bills = await _billService.GetAllBillsAsync();
            return Ok(usage.Select(u => ToDto(u, bills)));
        }

        // GET: api/waterusage/customer/C001  (Customer: View Current Usage, Partner: Retrieve Usage Details, Staff, QuickPayUser)
        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Customer,Partner,Staff,QuickPayUser")]
        public async Task<IActionResult> GetUsageByCustomer(string customerId)
        {
            if (!User.CanAccessCustomer(customerId))
                return Forbid();

            var usage = await _usageService.GetUsageByCustomerIdAsync(customerId);
            if (!usage.Any())
                return NotFound($"No water usage records found for customer {customerId}.");

            // The BillId on each reading is a display-only convenience (see ToDto) - it must
            // never be able to take down the meter-reading list itself (which "Load Billing
            // Periods" depends on), so a problem in the Bill read path degrades to "BillId
            // unknown" instead of failing this whole request.
            var bills = await TryGetBillsAsync(customerId);
            return Ok(usage.Select(u => ToDto(u, bills)));
        }

        // GET: api/waterusage/customer/C001/unbilled  (Staff use case: Generate Bill -> Load
        // Billing Periods). Returns exactly the recorded meter readings for this customer
        // that do not already have a Bill for the same Customer ID + Billing Period -
        // computed server-side from the actual WaterUsage/Bill records (WaterUsageService.
        // GetUnbilledUsageAsync), never hard-coded and never duplicated in the frontend.
        [HttpGet("customer/{customerId}/unbilled")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetUnbilledUsage(string customerId)
        {
            var usage = await _usageService.GetUnbilledUsageAsync(customerId);

            // These are unbilled by definition, so BillId is always null here - no bill
            // lookup needed for this endpoint.
            return Ok(usage.Select(u => new WaterUsageDto
            {
                UsageId = u.UsageId,
                CustomerId = u.CustomerId,
                BillingPeriod = u.BillingPeriod,
                UnitsConsumed = u.UnitsConsumed,
                ReadingDate = u.ReadingDate,
                BillId = null
            }));
        }

        // GET: api/waterusage/customer/C001/latest
        [HttpGet("customer/{customerId}/latest")]
        [Authorize(Roles = "Customer,Partner,Staff,QuickPayUser")]
        public async Task<IActionResult> GetLatestUsage(string customerId)
        {
            if (!User.CanAccessCustomer(customerId))
                return Forbid();

            var usage = await _usageService.GetLatestUsageAsync(customerId);
            if (usage == null)
                return NotFound($"No water usage records found for customer {customerId}.");

            var bills = await TryGetBillsAsync(customerId);
            return Ok(ToDto(usage, bills));
        }

        // POST: api/waterusage  (Staff use case: Record Meter Reading)
        // Validates the customer exists, units consumed isn't negative and no reading already
        // exists for this customer + billing period before creating the record (see
        // WaterUsageService.RecordUsageAsync) - the same validate-then-create pattern
        // BillService.GenerateBillAsync uses for bills.
        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> CreateUsage([FromBody] CreateWaterUsageDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (success, message, usage) = await _usageService.RecordUsageAsync(
                dto.CustomerId, dto.BillingPeriod, dto.UnitsConsumed, dto.ReadingDate);

            if (!success || usage == null)
                return BadRequest(new { message });

            // A freshly-recorded reading is normally billed later (Staff generates the bill
            // afterwards), so this lookup is just for consistency in case a bill for this
            // period already exists. The reading has already been saved at this point
            // (RecordUsageAsync), so a problem here must never turn a successful recording
            // into an error response - it only affects the optional BillId on the reply.
            var bills = await TryGetBillsAsync(usage.CustomerId);
            return CreatedAtAction(nameof(GetLatestUsage), new { customerId = usage.CustomerId }, ToDto(usage, bills));
        }

        // Best-effort Bill lookup used only to annotate a WaterUsageDto's BillId. Never lets
        // a problem in the Bill read path fail a meter-reading endpoint - falls back to "no
        // bills known" (BillId shows as null) instead.
        private async Task<IEnumerable<Bill>> TryGetBillsAsync(string customerId)
        {
            try
            {
                return await _billService.GetBillsByCustomerIdAsync(customerId);
            }
            catch
            {
                return Enumerable.Empty<Bill>();
            }
        }

        // Resolves the Bill ID for a usage record by matching CustomerId + BillingPeriod
        // against the Bill table - the two tables aren't linked by a foreign key, so this
        // is done here rather than stored/hardcoded on WaterUsage itself. Returns null when
        // no bill has been generated yet for that reading's billing period.
        private static WaterUsageDto ToDto(WaterUsage usage, IEnumerable<Bill> bills)
        {
            var matchingBill = bills.FirstOrDefault(b =>
                b.CustomerId == usage.CustomerId && b.BillingPeriod == usage.BillingPeriod);

            return new WaterUsageDto
            {
                UsageId = usage.UsageId,
                CustomerId = usage.CustomerId,
                BillingPeriod = usage.BillingPeriod,
                UnitsConsumed = usage.UnitsConsumed,
                ReadingDate = usage.ReadingDate,
                BillId = matchingBill?.BillId
            };
        }
    }
}
