using MediatR;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Queries;

/// <summary>
/// The clinic's wallet: balance, threshold and the newest ledger rows. Clinic-scoped — the
/// applicationId comes from the request context, never from the caller.
/// </summary>
public sealed record GetWalletQuery(Guid ApplicationId) : IRequest<ClinicWalletDto>;
