using Microsoft.EntityFrameworkCore;
using NWSDB.API.Models;

namespace NWSDB.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Customer accounts are stored in the database (not in memory), so a registered customer
        // and their Customer ID survive an API restart and can never be handed out twice.
        public DbSet<Customer> Customers => Set<Customer>();

        public DbSet<Bill> Bills => Set<Bill>();

        public DbSet<WaterUsage> WaterUsages => Set<WaterUsage>();

        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();

        public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Customer>(e =>
            {
                e.ToTable("Customers");
                e.HasKey(c => c.CustomerId);
            });

            

            modelBuilder.Entity<WaterUsage>().HasData(
                new WaterUsage { UsageId = 1, CustomerId = "C001", BillingPeriod = "2026-08", UnitsConsumed = 18.5m, ReadingDate = new DateTime(2026, 9, 1) },
                new WaterUsage { UsageId = 2, CustomerId = "C002", BillingPeriod = "2026-08", UnitsConsumed = 12.0m, ReadingDate = new DateTime(2026, 9, 1) },
                new WaterUsage { UsageId = 3, CustomerId = "C001", BillingPeriod = "2026-07", UnitsConsumed = 16.2m, ReadingDate = new DateTime(2026, 8, 1) }
            );
        }
    }
}

