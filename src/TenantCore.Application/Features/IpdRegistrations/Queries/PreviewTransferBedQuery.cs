using MediatR;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdRegistrations.Queries;

// DEF-IPD-16: read-only preview so the Shift dialog can show the standard amount, the
// grace-adjusted suggestion and the manual amount's cap before the user commits to a shift.
public sealed record PreviewTransferBedQuery(
    Guid Id,
    Guid ApplicationId,
    Guid NewBedId) : IRequest<TransferBedPreviewDto>;
