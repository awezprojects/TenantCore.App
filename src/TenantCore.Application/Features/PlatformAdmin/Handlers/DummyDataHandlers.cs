using MediatR;
using Microsoft.Extensions.Logging;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;
using TenantCore.Application.Features.PlatformAdmin.Queries;
using TenantCore.Shared.Dtos.PlatformAdmin;

namespace TenantCore.Application.Features.PlatformAdmin.Handlers;

/// <summary>
/// Read-only counts for the Admin portal's Test Data page. Nothing is written, and no guard lives
/// here — DummyData:Enabled is enforced by InternalDummyDataController before the query is sent.
/// </summary>
public sealed class GetDummyDataStatusHandler(IDummyDataStore store)
    : IRequestHandler<GetDummyDataStatusQuery, DummyDataStatusDto>
{
    public Task<DummyDataStatusDto> Handle(GetDummyDataStatusQuery query, CancellationToken cancellationToken)
        => store.GetStatusAsync(query.ApplicationId, cancellationToken);
}

/// <summary>
/// Seeds the canonical dummy set. The store refuses (InvalidOperationException → 409) when the clinic
/// already holds dummy data, so a second seed can never double up on a live clinic.
/// </summary>
public sealed class SeedDummyDataHandler(IDummyDataStore store, ILogger<SeedDummyDataHandler> logger)
    : IRequestHandler<SeedDummyDataCommand, DummyDataStatusDto>
{
    public async Task<DummyDataStatusDto> Handle(SeedDummyDataCommand command, CancellationToken cancellationToken)
    {
        var status = await store.SeedAsync(command.ApplicationId, cancellationToken);

        // Counts only — the seeded rows are deliberately anonymous (ADR-011).
        logger.LogWarning(
            "Dummy data seeded into clinic {ApplicationId} by admin {AdminUserId}: patients={Patients}, wards={Wards}, rooms={Rooms}, beds={Beds}, particulars={Particulars}, opd={Opd}, prescriptions={Prescriptions}.",
            command.ApplicationId, command.AdminUserId,
            status.PatientCount, status.WardCount, status.RoomCount, status.BedCount,
            status.ParticularCount, status.OpdRegistrationCount, status.PrescriptionCount);

        return status;
    }
}

/// <summary>
/// Cascade-deletes every dummy record. The store refuses (InvalidOperationException → 409) when there
/// is nothing to purge, and rolls the whole delete back if any FK it did not know about blocks it.
/// </summary>
public sealed class PurgeDummyDataHandler(IDummyDataStore store, ILogger<PurgeDummyDataHandler> logger)
    : IRequestHandler<PurgeDummyDataCommand, DummyDataStatusDto>
{
    public async Task<DummyDataStatusDto> Handle(PurgeDummyDataCommand command, CancellationToken cancellationToken)
    {
        var status = await store.PurgeAsync(command.ApplicationId, cancellationToken);

        logger.LogWarning(
            "Dummy data purged from clinic {ApplicationId} by admin {AdminUserId}. Remaining counts: patients={Patients}, wards={Wards}, rooms={Rooms}, beds={Beds}, particulars={Particulars}.",
            command.ApplicationId, command.AdminUserId,
            status.PatientCount, status.WardCount, status.RoomCount, status.BedCount, status.ParticularCount);

        return status;
    }
}
