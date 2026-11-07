using QuickPay.PartnerApp;

// ---------------------------------------------------------------------------------------------
// QuickPay Lanka - a third-party bill-payment portal.
//
// This is NOT part of the NWSDB system. It is its own web application (own server, own page,
// own port 5200) with its OWN login system: a QuickPay email/password, completely separate
// from a customer's NWSDB account, checked against QuickPay's own account store on NWSDB
// (POST /api/auth/quickpay/login) - never against a customer's NWSDB password.
//
// Every QuickPay account is linked, on the NWSDB side, to the specific NWSDB Customer ID(s)
// it is allowed to act for. That mapping is decided when the account is set up (see
// DirectoryStore.QuickPayAccounts) and is signed into the JWT at login as
// "quickpay_authorized_customer" claims - QuickPay's server and browser never get to state
// which Customer ID they want; they can only read back what NWSDB already decided.
//
//   1. logs the QuickPay account in with its OWN email/password -> POST /api/auth/quickpay/login
//   2. every request after that carries that account's own JWT, which already carries the
//      authorized Customer ID(s) as claims
//   3. loads that customer's bill / usage                        -> GET  /api/customer, /api/bill, /api/waterusage
//   4. pays the exact bill amount (SIMULATED payment)             -> POST /api/payment/simulate
// Step 4 is what records the payment and marks the bill as Paid inside the NWSDB main system.
// NWSDB remains the single source of truth for bills and payments: QuickPay never stores a
// bill amount, a paid status, or a payment record of its own - every screen is a live read
// from NWSDB, and every payment is a live write to NWSDB, always re-checked there against the
// caller's own signed claims - never against a Customer ID the browser supplies.
//
// There is no real payment gateway: this is an academic/demo project. Card fields are only
// forwarded to NWSDB for demo validation - QuickPay never stores or logs them.
// ---------------------------------------------------------------------------------------------
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<MainSystemOptions>(builder.Configuration.GetSection("MainSystem"));
builder.Services.AddSingleton<MainSystemClient>();

var app = builder.Build();

// "NWSDB unreachable" -> a clean JSON error for the web page.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (MainSystemException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

app.UseDefaultFiles();
app.UseStaticFiles();

static IResult Relay(MainResponse r) =>
    r.Ok ? Results.Text(r.Text, "application/json")
         : Results.Json(new { message = r.Message }, statusCode: (int)r.Status);

// Pulls "Bearer <token>" out of the incoming Authorization header. Every /api/me/... endpoint
// below requires this - there is no other way in, and no endpoint accepts a Customer ID from
// the browser (URL, body or otherwise) as a substitute for it.
static bool TryGetBearerToken(HttpRequest request, out string token)
{
    var header = request.Headers["Authorization"].ToString();
    if (!string.IsNullOrEmpty(header) && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        token = header["Bearer ".Length..].Trim();
        if (token.Length > 0) return true;
    }
    token = string.Empty;
    return false;
}

// Extracts the token and this account's primary authorized Customer ID together, since
// almost every /api/me/... endpoint needs both.
static bool TryGetAuthorizedCustomer(HttpRequest request, out string token, out string customerId)
{
    customerId = string.Empty;
    if (!TryGetBearerToken(request, out token))
        return false;

    // This demo links one QuickPay account to exactly one NWSDB Customer ID (see
    // DirectoryStore.QuickPayAccounts), so the first authorized claim is "the" customer.
    // If an account were ever linked to more than one, this is the one place that would
    // need to grow into a customer picker - every downstream call already re-validates
    // against ALL of the account's authorized claims, not just this first one.
    var authorized = JwtPayload.GetAuthorizedCustomerIds(token);
    if (authorized.Count == 0) return false;

    customerId = authorized[0];
    return true;
}

// POST /api/session/login  (QuickPay Login)
// Relayed straight through to NWSDB's QuickPay-specific login endpoint - a completely
// separate authentication system from Customer Login. QuickPay does not check this
// email/password itself, does not keep it anywhere beyond this one forwarded request, and
// this is never the same password as the customer's own NWSDB account.
app.MapPost("/api/session/login", async (LoginRequest req, MainSystemClient main) =>
{
    var reply = await main.SendAnonymousAsync(HttpMethod.Post, "api/auth/quickpay/login",
        new { email = req.Email, password = req.Password });
    return Relay(reply);
});

// GET /api/me/dashboard  (Customer Dashboard / "My Current Bill")
// The Customer ID is never accepted from the browser: it comes from the authorized-customer
// claim(s) NWSDB signed into this QuickPay account's JWT at login, and that SAME token is
// forwarded on every downstream NWSDB call, so NWSDB independently re-verifies the signature
// and re-checks ownership itself.
app.MapGet("/api/me/dashboard", async (HttpRequest request, MainSystemClient main) =>
{
    if (!TryGetAuthorizedCustomer(request, out var token, out var customerId))
        return Results.Json(new { message = "Please log in." }, statusCode: 401);

    var id = Uri.EscapeDataString(customerId);

    var customer = await main.SendAsUserAsync(HttpMethod.Get, $"api/customer/{id}", token);
    if (customer.Unauthorized)
        return Results.Json(new { message = "Your session has expired. Please log in again." }, statusCode: 401);
    if (!customer.Ok) return Relay(customer);

    var bill = await main.SendAsUserAsync(HttpMethod.Get, $"api/bill/customer/{id}/current", token);
    if (!bill.Ok && !bill.NotFound) return Relay(bill);           // 404 just means "nothing currently due"

    var usage = await main.SendAsUserAsync(HttpMethod.Get, $"api/waterusage/customer/{id}/latest", token);
    if (!usage.Ok && !usage.NotFound) return Relay(usage);

    // All of this customer's bills (404 just means "no bills at all"). The page lists the
    // unpaid ones oldest-first; NWSDB still enforces the oldest-first rule when paying.
    var allBills = await main.SendAsUserAsync(HttpMethod.Get, $"api/bill/customer/{id}", token);
    if (!allBills.Ok && !allBills.NotFound) return Relay(allBills);

    return Results.Json(new
    {
        customer = customer.Json,
        bill = bill.Ok ? bill.Json : null,
        bills = allBills.Ok ? allBills.Json : null,
        latestUsage = usage.Ok ? usage.Json : null
    });
});

// POST /api/me/pay  (Pay Bill - SIMULATED)
// Only a Bill ID and payment-method/card fields come from the browser. The Customer ID comes
// from this QuickPay account's authorized-customer claim(s), never from the browser, and the
// amount is decided entirely by NWSDB from the bill itself - it is never sent in this request
// at all. NWSDB re-verifies both that the account is authorized for this customer AND that
// the bill belongs to that customer and is not already paid, before charging anything.
app.MapPost("/api/me/pay", async (PayRequest req, HttpRequest request, MainSystemClient main) =>
{
    if (!TryGetAuthorizedCustomer(request, out var token, out var customerId))
        return Results.Json(new { message = "Please log in." }, statusCode: 401);

    if (req.BillId <= 0)
        return Results.Json(new { message = "BillId is required." }, statusCode: 400);

    // No "channel" is sent here: NWSDB attributes this payment to QuickPay Lanka purely from
    // the authenticated QuickPayUser role on the token, so the browser has no way to claim a
    // different channel even if it tried.
    var reply = await main.SendAsUserAsync(HttpMethod.Post, "api/payment/simulate", token, new
    {
        customerId,
        billId = req.BillId,
        paymentMethod = req.PaymentMethod,
        cardNumber = req.CardNumber,
        expiryDate = req.ExpiryDate,
        cvv = req.Cvv
    });

    if (reply.Unauthorized)
        return Results.Json(new { message = "Your session has expired. Please log in again." }, statusCode: 401);

    return Relay(reply);
});

// GET /api/me/payments  (Payment History)
// Loaded live from NWSDB every time - never QuickPay's own cache - and scoped to the
// authorized customer the same way every other /api/me/... endpoint is. Only payments made
// THROUGH QuickPay Lanka are shown here - a customer may also have payments made directly on
// the NWSDB Customer Portal (or another channel), and those belong in NWSDB's own payment
// history, not QuickPay's, so they're filtered out below by PaidByPartnerId.
app.MapGet("/api/me/payments", async (HttpRequest request, MainSystemClient main) =>
{
    if (!TryGetAuthorizedCustomer(request, out var token, out var customerId))
        return Results.Json(new { message = "Please log in." }, statusCode: 401);

    var id = Uri.EscapeDataString(customerId);
    var reply = await main.SendAsUserAsync(HttpMethod.Get, $"api/payment/customer/{id}", token);

    if (reply.Unauthorized)
        return Results.Json(new { message = "Your session has expired. Please log in again." }, statusCode: 401);

    if (!reply.Ok || reply.Json is not { ValueKind: System.Text.Json.JsonValueKind.Array } payments)
        return Relay(reply);

    var quickPayOnly = payments.EnumerateArray()
        .Where(p => p.TryGetProperty("paidByPartnerId", out var v)
                 && v.ValueKind == System.Text.Json.JsonValueKind.String
                 && v.GetString() == "QuickPay Lanka")
        .ToList();

    return Results.Text(System.Text.Json.JsonSerializer.Serialize(quickPayOnly), "application/json");
});

app.Run();
