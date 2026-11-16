using MediatR;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Commands;

/// <summary>
/// Returns the clinic's currently open wallet top-up (pending or with a link already created), or
/// null if none is open. Link creation is asynchronous (a background task calls Razorpay), so the
/// client polls this after Create so the URL appears as soon as it exists, without the clinic admin
/// having to re-submit the same amount to see it.
/// </summary>
public sealed record GetActiveWalletTopUpQuery(Guid ApplicationId) : IRequest<WalletTopUpPaymentDto?>;
