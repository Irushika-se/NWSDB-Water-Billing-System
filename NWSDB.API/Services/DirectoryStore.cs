using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    /// <summary>
    /// In-memory singleton store for actors that aren't backed by EF Core yet
    /// (Partner, Staff and QuickPay account directory + login credentials).
    /// Customers are NOT here any more: they live in the database (ApplicationDbContext.Customers).
    /// Registered once in Program.cs so PartnerController and AuthController
    /// read/write the SAME data instead of separate static lists.
    /// Swap for real DB-backed tables later without changing the controllers' public API.
    /// </summary>
    public class DirectoryStore
    {
        public List<Partner> Partners { get; } = new()
        {
            // The demo QuickPay partner app (QuickPay.PartnerApp/appsettings.json) already has the
            // raw key "pk_quickpay_001" configured, so only its hash is kept here - exactly how a
            // real partner's key would be stored after PartnerController.CreatePartner issues it.
            new Partner {
                PartnerId = "P001",
                PartnerName = "QuickPay Lanka",
                ContactEmail = "support@quickpay.lk",
                PhoneNumber = "011-2345678",
                PartnerType = PartnerTypes.PaymentGateway,
                BusinessAddress = "42 Galle Road, Colombo 03",
                BusinessRegistrationNo = "PV 00123456",
                Description = "Mobile payment gateway partner for online bill payments.",
                ApiKeyHash = ApiKeyHasher.Hash("pk_quickpay_001")
            }
        };

        public List<Staff> StaffMembers { get; } = new()
        {
            new Staff { StaffId = "S001", Email = "staff@nwsdb.lk", Password = "staff123", Name = "Staff Admin" }
        };

        // QuickPay Lanka's OWN login accounts - completely separate from a Customer's NWSDB
        // account (different email, different password, its own credential store). Each
        // account is explicitly linked to the one NWSDB Customer ID it is allowed to act for;
        // that mapping is decided here on the server, never by anything the browser sends.
        public List<QuickPayAccount> QuickPayAccounts { get; } = new()
        {
            new QuickPayAccount { QuickPayAccountId = "QP001", Email = "kamal.q@quickpay.lk", Password = "qp-pass1", DisplayName = "Kamal Perera", AuthorizedCustomerIds = new() { "C001" } },
            new QuickPayAccount { QuickPayAccountId = "QP002", Email = "nimal.q@quickpay.lk", Password = "qp-pass2", DisplayName = "Nimal Silva", AuthorizedCustomerIds = new() { "C002" } },
            new QuickPayAccount { QuickPayAccountId = "QP003", Email = "sunil.q@quickpay.lk", Password = "qp-pass3", DisplayName = "Sunil Fernando", AuthorizedCustomerIds = new() { "C003" } },
            new QuickPayAccount { QuickPayAccountId = "QP004", Email = "dilani.q@quickpay.lk", Password = "qp-pass4", DisplayName = "Dilani Wickramasinghe", AuthorizedCustomerIds = new() { "C004" } }
        };
    }
}
