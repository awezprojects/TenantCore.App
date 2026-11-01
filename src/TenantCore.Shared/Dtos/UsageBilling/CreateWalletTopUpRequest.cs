namespace TenantCore.Shared.Dtos.UsageBilling;

/// <summary>
/// The clinic's own top-up request. There is deliberately no <c>ApplicationId</c> and no payer
/// detail here: the clinic comes from the <c>X-Application-Id</c> header and the payer from the
/// authenticated user, so neither can be spoofed.
/// </summary>
public record CreateWalletTopUpRequest
{
    public decimal Amount { get; init; }
}
