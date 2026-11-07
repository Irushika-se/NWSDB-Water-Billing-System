using Microsoft.EntityFrameworkCore;
using NWSDB.API.Models;

namespace NWSDB.API.Data
{
    /// <summary>
    /// Inserts the extra demo rows (see DemoSeedData) when the API starts.
    /// Idempotent: a row is only added if that customer + billing period isn't already
    /// there, so restarting the API never creates duplicates. Works on an existing
    /// database with no new EF migration needed.
    /// </summary>
    public static class DbSeeder
    {
        /// <summary>
        /// EnsureCreated does nothing when the database already exists, so a database created
        /// before Customers moved into it would have no Customers table. This adds the table
        /// only if it is missing (a brand-new database already gets it from EnsureCreated).
        /// </summary>
        public static Task EnsureCustomersTableAsync(ApplicationDbContext db) =>
            db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[dbo].[Customers]', N'U') IS NULL
CREATE TABLE [dbo].[Customers] (
    [CustomerId] nvarchar(450) NOT NULL,
    [Name] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [Phone] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Customers] PRIMARY KEY ([CustomerId])
);");

        /// <summary>
        /// EnsureCreated also does nothing to a database that already exists, so a database
        /// created before the AddBillIssuedDate migration was added would be missing the
        /// [Bills].[IssuedDate] column that Bill model now requires on every read (GetAllBills,
        /// GetBillsByCustomerId, GetBillById, GetCurrentBill all select it) - those queries would
        /// throw "Invalid column name 'IssuedDate'" on such a database. Adds the column (and
        /// backfills existing rows from DueDate, a reasonable approximation) only if it is
        /// missing; a no-op on a database that already has it.
        /// </summary>
        public static Task EnsureBillIssuedDateColumnAsync(ApplicationDbContext db) =>
            db.Database.ExecuteSqlRawAsync(@"
IF COL_LENGTH(N'dbo.Bills', N'IssuedDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[Bills] ADD [IssuedDate] datetime2 NOT NULL
        CONSTRAINT [DF_Bills_IssuedDate_Backfill] DEFAULT '0001-01-01T00:00:00.000';
    UPDATE [dbo].[Bills] SET [IssuedDate] = [DueDate];
    ALTER TABLE [dbo].[Bills] DROP CONSTRAINT [DF_Bills_IssuedDate_Backfill];
END");

        public static async Task SeedAsync(ApplicationDbContext db)
        {
            foreach (var customer in DemoSeedData.GetDemoCustomers())
            {
                var exists = await db.Customers.AnyAsync(c => c.CustomerId == customer.CustomerId);
                if (!exists) db.Customers.Add(customer);
            }

            foreach (var bill in DemoSeedData.GetExtraBills())
            {
                var exists = await db.Bills.AnyAsync(b =>
                    b.CustomerId == bill.CustomerId && b.BillingPeriod == bill.BillingPeriod);
                if (!exists) db.Bills.Add(bill);
            }

            foreach (var usage in DemoSeedData.GetExtraUsage())
            {
                var exists = await db.WaterUsages.AnyAsync(u =>
                    u.CustomerId == usage.CustomerId && u.BillingPeriod == usage.BillingPeriod);
                if (!exists) db.WaterUsages.Add(usage);
            }

            // Save bills first: the payments below need the bills' generated IDs.
            await db.SaveChangesAsync();

            foreach (var demo in DemoSeedData.GetExtraPayments())
            {
                var bill = await db.Bills.FirstOrDefaultAsync(b =>
                    b.CustomerId == demo.CustomerId && b.BillingPeriod == demo.BillingPeriod);
                if (bill == null) continue;

                // One payment per bill - never duplicated on restart.
                var exists = await db.Payments.AnyAsync(p => p.BillId == bill.BillId);
                if (exists) continue;

                db.Payments.Add(new Payment
                {
                    CustomerId = bill.CustomerId,
                    BillId = bill.BillId,
                    AmountPaid = bill.Amount,
                    PaymentDate = demo.PaymentDate,
                    PaymentMethod = demo.PaymentMethod,
                    PaidByPartnerId = demo.PaidBy,
                    GatewayReference = demo.GatewayReference
                });
            }

            await db.SaveChangesAsync();

            foreach (var request in DemoSeedData.GetExtraSupportRequests())
            {
                var exists = await db.SupportRequests.AnyAsync(r =>
                    r.CustomerId == request.CustomerId && r.Subject == request.Subject);
                if (!exists) db.SupportRequests.Add(request);
            }

            foreach (var message in DemoSeedData.GetExtraContactMessages())
            {
                var exists = await db.ContactMessages.AnyAsync(m =>
                    m.Email == message.Email && m.Subject == message.Subject);
                if (!exists) db.ContactMessages.Add(message);
            }

            await db.SaveChangesAsync();
        }
    }
}
