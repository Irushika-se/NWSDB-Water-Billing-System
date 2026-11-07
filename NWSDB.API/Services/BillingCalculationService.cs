using Microsoft.Extensions.Options;
using NWSDB.API.Models;

namespace NWSDB.API.Services
{
    /// <summary>
    /// The single, existing billing/tariff calculation service the project has for turning
    /// water usage into a bill amount. BillService.GenerateBillAsync is the only caller -
    /// there is no second/duplicate calculation anywhere else (in particular NOT in the
    /// frontend), so this is always the source of truth for the amount.
    /// </summary>
    public class BillingCalculationService : IBillingCalculationService
    {
        private readonly BillingTariffOptions _tariff;

        public BillingCalculationService(IOptions<BillingTariffOptions> tariff)
        {
            _tariff = tariff.Value;
        }

        public decimal CalculateAmount(decimal unitsConsumed)
        {
            if (unitsConsumed < 0)
                throw new ArgumentOutOfRangeException(nameof(unitsConsumed), "Water usage cannot be negative.");

            var amount = _tariff.FixedServiceChargeRs;
            var remaining = unitsConsumed;
            var previousBound = 0m;

            foreach (var tier in _tariff.Tiers)
            {
                if (remaining <= 0) break;

                // The last tier (UpToUnits == null) absorbs whatever usage is left.
                var tierCapacity = tier.UpToUnits.HasValue
                    ? tier.UpToUnits.Value - previousBound
                    : remaining;

                var unitsInThisTier = Math.Min(remaining, Math.Max(tierCapacity, 0));
                if (unitsInThisTier > 0)
                {
                    amount += unitsInThisTier * tier.RatePerUnit;
                    remaining -= unitsInThisTier;
                }

                if (tier.UpToUnits.HasValue)
                    previousBound = tier.UpToUnits.Value;
            }

            return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        }

        public DateTime CalculateDueDate(DateTime issuedDate)
        {
            return issuedDate.AddDays(_tariff.DueDateDays);
        }
    }
}
