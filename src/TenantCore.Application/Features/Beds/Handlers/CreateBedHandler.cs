using MediatR;
using TenantCore.Application.Features.Beds.Commands;
using TenantCore.Application.Features.Beds.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Errors;

namespace TenantCore.Application.Features.Beds.Handlers;

public sealed class CreateBedHandler(IBedRepository bedRepository, IRoomRepository roomRepository)
    : IRequestHandler<CreateBedCommand, BedDto>
{
    public async Task<BedDto> Handle(CreateBedCommand request, CancellationToken cancellationToken)
    {
        var room = await roomRepository.GetByIdWithBedsAsync(request.RoomId, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Room), request.RoomId);

        if (await bedRepository.ExistsByNumberAsync(request.RoomId, request.BedNumber, cancellationToken: cancellationToken))
            throw new InvalidOperationException(UserMessages.BedNumberTaken);

        var bed = Bed.Create(request.ApplicationId, room.WardId, request.RoomId, request.BedNumber);
        await bedRepository.AddAsync(bed, cancellationToken);
        await bedRepository.SaveChangesAsync(cancellationToken);
        return BedTranslator.ToDto(bed);
    }
}
