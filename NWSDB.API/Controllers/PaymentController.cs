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
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IBillService _billService;

        public PaymentController(IPaymentService paymentService, IBillService billService)
        {
            _paymentService = paymentService;
            _billService = billService;
        }

        // GET: api/payment  (Staff use case: View Payment Details)
        [HttpGet]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetAllPayments()
        {
            var payments = await _paymentService.GetAllPaymentsAsync();
            return Ok(payments.Select(p => ToDto(p)));
        }

        // GET: api/payment/5
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> GetPaymentById(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
                return NotFound($"Payment with ID {id} was not found.");

            return Ok(ToDto(payment));
        }

        // GET: api/payment/customer/C001
        // Customer: View Payment History. Partner: Retrieve Payment Details. Staff: View Payment
        // Details. QuickPayUser: payment history for an authorized customer.
        [HttpGet("customer/{customerId}")]
        [Authorize(Roles = "Customer,Partner,Staff,QuickPayUser")]
        public async Task<IActionResult> GetPaymentsByCustomer(string customerId)
        {
            if (!User.CanAccessCustomer(customerId))
                return Forbid();

            var payments = await _paymentService.GetPaymentsByCustomerIdAsync(customerId);
            return Ok(payments.Select(p => ToDto(p)));
        }

        // POST: api/payment/simulate
        // Process Payment -> Generate Payment Confirmation, as a SIMULATED payment (demo
        // project - there is no real payment gateway). The card fields are only used to
        // validate the demo form in this one request; they are never stored or logged.
        // The amount is never taken from the client: the bill's own amount is charged.
        [HttpPost("simulate")]
        [Authorize(Roles = "Customer,Partner,QuickPayUser")]
        public async Task<IActionResult> SimulatePayment([FromBody] SimulatePaymentDto dto)
        {
            var role = User.GetRole();
            var customerId = (dto.CustomerId ?? string.Empty).Trim();

            // A Customer may only pay their own bill. A QuickPayUser may only pay for a
            // Customer ID its account is authorized for (baked into its JWT at login - see
            // AuthController.QuickPayLogin). A Partner may pay on behalf of any customer
            // (matching the original Make Bill Payment use case). CanAccessCustomer covers
            // all three cases from the JWT alone - the request body's CustomerId is never
            // trusted on its own, only checked against what the token actually authorizes.
            if (!User.CanAccessCustomer(customerId))
                return Forbid();

            // "Paid By Partner": which channel this payment came through. Decided entirely
            // from the authenticated role - never from anything the client sends - so a
            // QuickPayUser payment is always correctly attributed to QuickPay Lanka and can
            // never claim to be a different channel.
            var paidBy = role switch
            {
                "Partner" => User.GetUserId() ?? "Partner",
                "QuickPayUser" => PaymentSources.QuickPayLanka,
                _ => PaymentSources.OnlinePortal
            };

            var card = new SimulatedCardDetails
            {
                CardNumber = dto.CardNumber,
                ExpiryDate = dto.ExpiryDate,
                Cvv = dto.Cvv
            };

            var (success, message, payment) = await _paymentService.ProcessSimulatedPaymentAsync(
                customerId, dto.BillId, dto.PaymentMethod, card, paidBy);

            if (!success || payment == null)
                return BadRequest(new { message });

            return Ok(ToDto(payment, message));
        }

        // PUT: api/payment/5/status  (Staff use case: Update Payment Status)
        // Lets Staff correct/reverse a payment's effect on its bill — e.g. a gateway
        // chargeback, a manual reconciliation, or fixing a mistaken "paid" flag.
        [HttpPut("{id:int}/status")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusDto dto)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
                return NotFound($"Payment with ID {id} was not found.");

            var bill = await _billService.GetBillByIdAsync(payment.BillId);
            if (bill == null)
                return NotFound($"Bill with ID {payment.BillId} was not found for this payment.");

            var updated = dto.IsPaid
                ? await _billService.MarkBillAsPaidAsync(bill.BillId)
                : await _billService.SetBillUnpaidAsync(bill.BillId); // reopen the bill (e.g. reversed/failed payment)

            return Ok(new { message = $"Bill {bill.BillId} status updated.", isPaid = dto.IsPaid });
        }

        private static PaymentDto ToDto(Payment payment, string message = "")
        {
            return new PaymentDto
            {
                PaymentId = payment.PaymentId,
                CustomerId = payment.CustomerId,
                BillId = payment.BillId,
                AmountPaid = payment.AmountPaid,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                PaidByPartnerId = payment.PaidByPartnerId,
                GatewayReference = payment.GatewayReference,
                Status = PaymentStatuses.Success,
                Message = message
            };
        }
    }
}
