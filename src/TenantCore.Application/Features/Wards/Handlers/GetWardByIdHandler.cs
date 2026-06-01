using MediatR;
using TenantCore.Application.Features.Wards.Queries;
using TenantCore.Application.Features.Wards.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Wards.Handlers;

public sealed class GetWardByIdHandler(IWardRepository repository) : IRequestHandler<GetWardByIdQuery, WardDto>
{
    public async Task<WardDto> Handle(GetWardByIdQuery request, CancellationToken cancellationToken)
    {
        var ward = await repository.GetByIdWithRoomsAsync(request.Id, request.ApplicationId, cancellationToken)
            ?? throw new NotFoundException(nameof(Ward), request.Id);
        return WardTranslator.ToDto(ward);
    }
}
