using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.OpdPayments.Translators;

public static class OpdPaymentTranslator
{
    public static OpdPaymentDto ToDto(OpdPayment entity) => new()
    {
        Id = entity.Id,
        ApplicationId = entity.ApplicationId,
        OpdRegistrationId = entity.OpdRegistrationId,
        VisitFee = entity.VisitFee,
        ParticularsTotal = entity.ParticularsTotal,
        TotalAmount = entity.TotalAmount,
        Discount = entity.Discount,
        FinalAmount = entity.FinalAmount,
        PaymentStatus = entity.PaymentStatus,
        AmountReceivedAt = entity.AmountReceivedAt,
        ReceivedByUserId = entity.ReceivedByUserId,
        CounterSessionId = entity.CounterSessionId
    };
}
