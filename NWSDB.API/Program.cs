using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NWSDB.API.Data;
using NWSDB.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddOpenApi();

// EF Core + SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Service layer (Dependency Injection)
builder.Services.AddScoped<IBillService, BillService>();
builder.Services.AddScoped<IWaterUsageService, WaterUsageService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ISupportRequestService, SupportRequestService>();
builder.Services.AddScoped<IContactMessageService, ContactMessageService>();

// NWSDB billing/tariff configuration (appsettings.json: "BillingTariff") + the service that
// calculates a bill's amount and due date from it. Staff use case: Generate Bill - the
// backend is the source of truth for the calculated amount, never the frontend or Staff.
builder.Services.Configure<NWSDB.API.Models.BillingTariffOptions>(builder.Configuration.GetSection("BillingTariff"));
builder.Services.AddSingleton<IBillingCalculationService, BillingCalculationService>();

// SIMULATED payment gateway (academic/demo project). No real payment provider is used -
// it just validates the demo card fields and approves the payment.
builder.Services.AddSingleton<IPaymentGatewayService, SimulatedPaymentGatewayService>();

// Shared in-memory directory (Partner/Staff/QuickPay accounts; customers are in the database) + JWT issuing
builder.Services.AddSingleton<DirectoryStore>();
builder.Services.AddSingleton<JwtTokenService>();

// JWT Bearer authentication — backs the Authenticate User / Authenticate Staff /
// Authenticate API Client use cases from the use-case diagram.
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// CORS - allows the NWSDB.Client (HTML/CSS/JS) frontend to call this API from the browser
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// First run: create the database + tables (this project has no EF Migrations folder, so
// nothing else would create them), then add the extra demo rows (customer C003's bills and
// water usage). EnsureCreated does nothing if the database already exists, and DbSeeder only
// inserts rows that are missing, so restarting the API is always safe.
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.EnsureCustomersTableAsync(db);
        // EnsureCreatedAsync only builds a brand-new database from the current model - on a
        // database that already existed (e.g. created before a later migration), it does
        // nothing, so a required column added since then (Bills.IssuedDate) can be silently
        // missing. This is a no-op if the column is already there.
        await DbSeeder.EnsureBillIssuedDateColumnAsync(db);
        await DbSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Could not create/seed the database. Check that SQL Server LocalDB is installed " +
                                "and that the connection string in appsettings.json is correct.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// The client calls http://localhost:5094. Redirecting to HTTPS in Development would break the
// browser's CORS preflight requests, so only redirect outside Development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
