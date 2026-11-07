using NWSDB.API.Models;

namespace NWSDB.API.Data
{
    /// <summary>
    /// Demo rows for all four customers - bills, water usage and payment history.
    /// (C001 and C002's water usage is seeded in ApplicationDbContext.HasData; their bills
    /// are seeded here along with everyone else's - see GetExtraBills below.)
    /// Kept as plain data with no EF dependency; DbSeeder inserts them at startup.
    ///
    /// The demo customers deliberately cover different states:
    ///   C001 - TWO unpaid/overdue bills (2026-08 and 2026-07); oldest bill (2026-06) is paid
    ///   C002 - everything paid (dashboard shows "No outstanding bill")
    ///   C003 - unpaid bill that is NOT yet due (status "Unpaid")
    ///   C004 - has data in EVERY customer tab: an unpaid current bill, four months of water
    ///          usage, and a payment history (Credit Card, Debit Card, and one paid via partner P001)
    /// </summary>
    public static class DemoSeedData
    {
        // The four demo customers (C001-C004). Stored in the Customers table by DbSeeder.
        public static List<Customer> GetDemoCustomers() => new()
        {
            new Customer { CustomerId = "C001", Name = "Kamal Perera", Address = "Colombo", Phone = "0771234567", Email = "kamal.perera@example.com", Password = "pass123" },
            new Customer { CustomerId = "C002", Name = "Nimal Silva", Address = "Kandy", Phone = "0712345678", Email = "nimal.silva@example.com", Password = "pass123" },
            new Customer { CustomerId = "C003", Name = "Sunil Fernando", Address = "Galle", Phone = "0723456789", Email = "sunil.fernando@example.com", Password = "pass123" },
            new Customer { CustomerId = "C004", Name = "Dilani Wickramasinghe", Address = "Matara", Phone = "0754567890", Email = "dilani.wickramasinghe@example.com", Password = "pass123" }
        };

        // Every demo bill for every demo customer, in ONE list ordered oldest billing period
        // first (ties broken by Customer ID). DbSeeder inserts this list in order and the
        // database assigns Bill IDs in that same order, so Bill ID and billing period now always
        // move together: the oldest bill in the system gets the lowest ID, the newest gets the
        // highest, for every customer - instead of some bills coming from a separately-numbered
        // fixed block (the old ApplicationDbContext.HasData) that didn't line up with this list.
        //
        // C001 also has TWO unpaid/overdue bills at once (2026-08 and 2026-07), to test the
        // "multiple outstanding bills" case on the Bill Payment / My Bills tabs - its oldest bill
        // (2026-06) is the one that's paid, matching the "always pay the oldest bill first" rule
        // the rest of the app enforces (BillingCalculationService / PaymentService).
        public static List<Bill> GetExtraBills() => new()
        {
            new Bill { CustomerId = "C004", BillingPeriod = "2026-05", Amount = 1095.75m, IssuedDate = new DateTime(2026, 6, 1), DueDate = new DateTime(2026, 6, 15), IsPaid = true },

            new Bill { CustomerId = "C001", BillingPeriod = "2026-06", Amount = 1180.00m, IssuedDate = new DateTime(2026, 7, 1), DueDate = new DateTime(2026, 7, 15), IsPaid = true },
            new Bill { CustomerId = "C004", BillingPeriod = "2026-06", Amount = 1180.50m, IssuedDate = new DateTime(2026, 7, 1), DueDate = new DateTime(2026, 7, 15), IsPaid = true },

            new Bill { CustomerId = "C001", BillingPeriod = "2026-07", Amount = 1100.00m, IssuedDate = new DateTime(2026, 8, 1), DueDate = new DateTime(2026, 8, 15), IsPaid = false },
            new Bill { CustomerId = "C003", BillingPeriod = "2026-07", Amount = 1305.00m, IssuedDate = new DateTime(2026, 8, 1), DueDate = new DateTime(2026, 8, 15), IsPaid = true },
            new Bill { CustomerId = "C004", BillingPeriod = "2026-07", Amount = 1240.00m, IssuedDate = new DateTime(2026, 8, 1), DueDate = new DateTime(2026, 8, 15), IsPaid = true },

            new Bill { CustomerId = "C001", BillingPeriod = "2026-08", Amount = 1250.50m, IssuedDate = new DateTime(2026, 9, 1), DueDate = new DateTime(2026, 9, 15), IsPaid = false },
            new Bill { CustomerId = "C002", BillingPeriod = "2026-08", Amount = 980.00m, IssuedDate = new DateTime(2026, 9, 1), DueDate = new DateTime(2026, 9, 15), IsPaid = true },
            new Bill { CustomerId = "C003", BillingPeriod = "2026-08", Amount = 1420.75m, IssuedDate = new DateTime(2026, 9, 1), DueDate = new DateTime(2026, 9, 30), IsPaid = false },
            new Bill { CustomerId = "C004", BillingPeriod = "2026-08", Amount = 1385.25m, IssuedDate = new DateTime(2026, 9, 1), DueDate = new DateTime(2026, 9, 30), IsPaid = false }
        };

        public static List<WaterUsage> GetExtraUsage() => new()
        {
            // C001: matches its 2026-06 bill (added above in GetExtraBills) - without
            // this record, the Water Usage tab would show no reading for June.
            new WaterUsage { CustomerId = "C001", BillingPeriod = "2026-06", UnitsConsumed = 15.4m, ReadingDate = new DateTime(2026, 7, 1) },

            new WaterUsage { CustomerId = "C003", BillingPeriod = "2026-08", UnitsConsumed = 21.3m, ReadingDate = new DateTime(2026, 9, 1) },
            new WaterUsage { CustomerId = "C003", BillingPeriod = "2026-07", UnitsConsumed = 19.8m, ReadingDate = new DateTime(2026, 8, 1) },

            new WaterUsage { CustomerId = "C004", BillingPeriod = "2026-08", UnitsConsumed = 22.4m, ReadingDate = new DateTime(2026, 9, 1) },
            new WaterUsage { CustomerId = "C004", BillingPeriod = "2026-07", UnitsConsumed = 20.1m, ReadingDate = new DateTime(2026, 8, 1) },
            new WaterUsage { CustomerId = "C004", BillingPeriod = "2026-06", UnitsConsumed = 18.9m, ReadingDate = new DateTime(2026, 7, 1) },
            new WaterUsage { CustomerId = "C004", BillingPeriod = "2026-05", UnitsConsumed = 17.3m, ReadingDate = new DateTime(2026, 6, 1) }
        };

        // Payment history for the PAID demo bills of every demo customer. The amount is taken from
        // the bill itself when DbSeeder inserts the row (so it always matches). No card data - just
        // method + reference. Unpaid bills (C001, C003 and C004's current bill) get no payment, so
        // they can still be paid in the demo.
        public static List<DemoPayment> GetExtraPayments() => new()
        {
            new DemoPayment("C001", "2026-06", "Credit Card", new DateTime(2026, 7, 12, 11, 20, 0), PaymentSources.OnlinePortal, "SIM-DEMO00000004"),
            new DemoPayment("C002", "2026-08", "Debit Card",  new DateTime(2026, 9, 10, 16, 45, 0), "P001",                      "SIM-DEMO00000005"),
            new DemoPayment("C003", "2026-07", "Debit Card",  new DateTime(2026, 8, 13, 9, 50, 0),  PaymentSources.OnlinePortal, "SIM-DEMO00000006"),

            new DemoPayment("C004", "2026-07", "Credit Card", new DateTime(2026, 8, 10, 10, 30, 0), PaymentSources.OnlinePortal, "SIM-DEMO00000001"),
            new DemoPayment("C004", "2026-06", "Debit Card",  new DateTime(2026, 7, 12, 14, 15, 0), PaymentSources.OnlinePortal, "SIM-DEMO00000002"),
            new DemoPayment("C004", "2026-05", "Debit Card",  new DateTime(2026, 6, 13, 9, 5, 0),   "P001",                      "SIM-DEMO00000003")
        };

        // Support Requests: gives customers C002, C003 and C004 full coverage on the
        // Support Request tab, and gives Staff something to see/respond to in every status
        // (New, In Progress, Resolved) on the Staff Support Requests screen.
        public static List<SupportRequest> GetExtraSupportRequests() => new()
        {
            new SupportRequest
            {
                CustomerId = "C002",
                Subject = "Low water pressure",
                Message = "Water pressure has been very low in my area for the past two days.",
                Status = SupportRequestStatuses.New,
                CreatedDate = new DateTime(2026, 9, 18, 9, 15, 0)
            },
            new SupportRequest
            {
                CustomerId = "C003",
                Subject = "Incorrect meter reading",
                Message = "This month's bill looks higher than usual, please recheck my meter reading.",
                Status = SupportRequestStatuses.InProgress,
                CreatedDate = new DateTime(2026, 9, 15, 14, 30, 0),
                StaffResponse = "We have scheduled a technician to re-check the meter on site."
            },
            new SupportRequest
            {
                CustomerId = "C004",
                Subject = "Request for pipeline inspection",
                Message = "There is a small leak near the main pipeline outside my house.",
                Status = SupportRequestStatuses.Resolved,
                CreatedDate = new DateTime(2026, 9, 5, 10, 0, 0),
                StaffResponse = "Leak inspected and repaired on 2026-09-07. Please let us know if it recurs."
            },
            new SupportRequest
            {
                CustomerId = "C004",
                Subject = "Billing address update",
                Message = "I have moved within the same city, please update my billing address.",
                Status = SupportRequestStatuses.New,
                CreatedDate = new DateTime(2026, 9, 20, 8, 45, 0)
            }
        };

        // Contact Messages: general public "Contact Us" enquiries (not tied to a logged-in
        // customer account), shown on the Staff Contact Messages tab in different statuses.
        public static List<ContactMessage> GetExtraContactMessages() => new()
        {
            new ContactMessage
            {
                Name = "Kamal Perera",
                Email = "kamal.perera@example.com",
                Subject = "General enquiry about new connection",
                Message = "I would like to know the process and cost for getting a new water connection.",
                Status = ContactMessageStatuses.New,
                CreatedAt = new DateTime(2026, 9, 19, 11, 0, 0)
            },
            new ContactMessage
            {
                Name = "Nimal Silva",
                Email = "nimal.silva@example.com",
                Subject = "Complaint about water quality",
                Message = "The tap water in our area has had an unusual smell for the last few days.",
                Status = ContactMessageStatuses.InProgress,
                CreatedAt = new DateTime(2026, 9, 16, 16, 20, 0)
            },
            new ContactMessage
            {
                Name = "Dilani Wickramasinghe",
                Email = "dilani.wickramasinghe@example.com",
                Subject = "Thank you for the quick repair",
                Message = "Just wanted to thank the team for fixing the pipeline leak so quickly.",
                Status = ContactMessageStatuses.Resolved,
                CreatedAt = new DateTime(2026, 9, 8, 13, 10, 0)
            }
        };
    }

    /// <summary>One demo payment row: which customer's bill period it pays, and how.</summary>
    public record DemoPayment(
        string CustomerId,
        string BillingPeriod,
        string PaymentMethod,
        DateTime PaymentDate,
        string PaidBy,
        string GatewayReference);
}
