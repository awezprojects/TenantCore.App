using MediatR;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// Returns one specific wallet top-up by id, at whatever status it's currently at — including Paid,
/// which GetActiveWalletTopUpQuery can no longer see once a payment stops being "open". The client
/// uses this to track one top-up all the way from link creation through to payment confirmation.
/// </summary>
public sealed record GetWalletTopUpStatusQuery(Guid ApplicationId, Guid PaymentId) : IRequest<WalletTopUpPaymentDto?>;
