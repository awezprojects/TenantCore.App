using MediatR;
using TenantCore.Shared.Dtos.UsageBilling;

namespace TenantCore.Application.Features.UsageBilling.Queries;

/// <summary>
/// Everything the billing tab shows in one read: mode (stored, effective and queued), coverage end,
/// wallet state, the live rates, and the registration fee with its due/waived/charged state.
/// </summary>
public sealed record GetClinicBillingSummaryQuery(Guid ApplicationId)
    : IRequest<ClinicBillingSummaryDto>;
