namespace NWSDB.API.Models
{
    /// <summary>
    /// One pricing band of the tariff: water usage above the previous tier's UpToUnits and
    /// up to this tier's UpToUnits is charged at RatePerUnit. The last tier in the list
    /// should leave UpToUnits null, meaning "everything above the previous tier".
    /// </summary>
    public class BillingTariffTier
    {
        public decimal? UpToUnits { get; set; }

        public decimal RatePerUnit { get; set; }
    }

    /// <summary>
    /// The NWSDB billing/tariff configuration, bound from appsettings.json ("BillingTariff").
    /// This is the single, existing source of truth BillingCalculationService uses to turn a
    /// water-usage figure into a bill amount and a due date - so Staff never types either one
    /// in, and the rates can be adjusted centrally without a code change.
    /// </summary>
    public class BillingTariffOptions
    {
        public decimal FixedServiceChargeRs { get; set; }

        public List<BillingTariffTier> Tiers { get; set; } = new();

        // How many days after the Issued Date a generated bill's Due Date falls.
        public int DueDateDays { get; set; } = 14;
    }
}
