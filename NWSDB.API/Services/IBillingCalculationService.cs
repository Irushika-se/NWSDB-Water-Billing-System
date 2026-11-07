namespace NWSDB.API.Services
{
    /// <summary>
    /// Turns a water-usage figure into a bill amount, and works out a bill's due date - both
    /// driven by the NWSDB billing/tariff configuration, never by a Staff-entered value.
    /// Used by BillService.GenerateBillAsync (Staff use case: Generate Bill).
    /// </summary>
    public interface IBillingCalculationService
    {
        decimal CalculateAmount(decimal unitsConsumed);

        DateTime CalculateDueDate(DateTime issuedDate);
    }
}
