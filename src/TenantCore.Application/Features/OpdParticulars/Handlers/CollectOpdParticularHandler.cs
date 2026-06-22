using MediatR;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;

namespace TenantCore.Application.Features.OpdParticulars.Handlers;

public sealed class CollectOpdParticularHandler(
    IOpdParticularRepository repository,
    IOpdPaymentRepository paymentRepository) : IRequestHandler<CollectOpdParticularCommand>
{
    public async Task Handle(CollectOpdParticularCommand request, CancellationToken cancellationToken)
    {
        var item = await repository.GetByIdAsync(request.ParticularId, cancellationToken);
        if (item is null || item.ApplicationId != request.ApplicationId)
            throw new NotFoundException(nameof(OpdParticular), request.ParticularId);

        // Individual collection is only allowed when no discount is applied.
        var payment = await paymentRepository.GetByOpdRegistrationIdAsync(
            item.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is not null && payment.Discount > 0)
            throw new InvalidOperationException(
                "A discount is applied. Collect the full discounted amount via 'Collect Full Bill'.");

        item.Collect(request.CollectedByUserId, request.CounterSessionId);
        repository.Update(item);
        await repository.SaveChangesAsync(cancellationToken);
    }
}
