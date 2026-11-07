namespace NWSDB.API.Services
{
    /// <summary>
    /// Models the "Payment Gateway" actor from the use-case diagram as a SIMULATION.
    /// This is an academic/demo project: no real card is ever charged and no external
    /// payment provider is contacted. The gateway only checks that the demo card details
    /// look plausible and, if they do, "approves" the charge with a generated reference.
    /// </summary>
    public interface IPaymentGatewayService
    {
        Task<SimulatedChargeResult> ChargeAsync(SimulatedCardDetails card, decimal amount);
    }

    /// <summary>
    /// Card details for ONE simulated request. Held in memory only - never stored, never
    /// logged (ToString is masked so an accidental log line cannot leak them).
    /// </summary>
    public sealed class SimulatedCardDetails
    {
        public string? CardNumber { get; init; }
        public string? ExpiryDate { get; init; }
        public string? Cvv { get; init; }

        public override string ToString() => "SimulatedCardDetails { CardNumber = ****, ExpiryDate = **/**, Cvv = *** }";
    }

    public sealed class SimulatedChargeResult
    {
        public bool Approved { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public string? Reference { get; private init; }

        public static SimulatedChargeResult Approve(string reference) =>
            new() { Approved = true, Message = "Simulated payment approved.", Reference = reference };

        public static SimulatedChargeResult Decline(string message) =>
            new() { Approved = false, Message = message };
    }
}
