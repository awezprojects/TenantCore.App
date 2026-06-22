using MediatR;
using TenantCore.Application.Features.OpdParticulars.Commands;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.OpdParticulars.Handlers;

public sealed class CollectAllOpdParticularsHandler(
    IOpdParticularRepository repository,
    IOpdPaymentRepository paymentRepository) : IRequestHandler<CollectAllOpdParticularsCommand>
{
    public async Task Handle(CollectAllOpdParticularsCommand request, CancellationToken cancellationToken)
    {
        // Individual batch collection is only allowed when no discount is applied.
        var payment = await paymentRepository.GetByOpdRegistrationIdAsync(
            request.OpdRegistrationId, request.ApplicationId, cancellationToken);
        if (payment is not null && payment.Discount > 0)
            throw new InvalidOperationException(
                "A discount is applied. Collect the full discounted amount via 'Collect Full Bill'.");

        var items = await repository.GetByOpdRegistrationIdAsync(
            request.OpdRegistrationId, request.ApplicationId, cancellationToken);

        var pending = items.Where(i => i.PaymentStatus == PaymentStatus.Pending).ToList();
        if (pending.Count == 0)
            return; // nothing to do

        foreach (var item in pending)
        {
            item.Collect(request.CollectedByUserId, request.CounterSessionId);
            repository.Update(item);
        }
        await repository.SaveChangesAsync(cancellationToken);
    }
}
