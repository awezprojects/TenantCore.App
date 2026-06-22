using MediatR;
using TenantCore.Application.Features.OpdPayments.Queries;
using TenantCore.Application.Features.OpdPayments.Translators;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Handlers;

public sealed class GetOpdPaymentByOpdIdHandler(IOpdPaymentRepository repository) : IRequestHandler<GetOpdPaymentByOpdIdQuery, OpdPaymentDto?>
{
    public async Task<OpdPaymentDto?> Handle(GetOpdPaymentByOpdIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await repository.GetByOpdRegistrationIdAsync(request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        return payment is null ? null : OpdPaymentTranslator.ToDto(payment);
    }
}
