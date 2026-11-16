using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.IpdPayments.Translators;

public static class IpdPaymentTranslator
{
    public static IpdProvisionalBillDto ToProvisionalBillDto(
        IpdRegistration registration, IpdPayment payment, IpdBillCalculationResult roomCharges, DateTime asOfUtc)
    {
        var totalAmount = payment.InitialFee + roomCharges.RoomChargesTotal + payment.ParticularsTotal;
        var finalAmount = totalAmount - payment.Discount;
        var balanceDue = finalAmount - payment.TotalDeposited;

        return new IpdProvisionalBillDto(
            IsProvisional: true,
            IpdRegistrationId: registration.Id,
            InitialFee: payment.InitialFee,
            RoomChargesTotal: roomCharges.RoomChargesTotal,
            RoomChargeLineItems: roomCharges.LineItems,
            ParticularsTotal: payment.ParticularsTotal,
            TotalAmount: totalAmount,
            Discount: payment.Discount,
            FinalAmount: finalAmount,
            TotalDeposited: payment.TotalDeposited,
            BalanceDue: balanceDue,
            AsOfUtc: asOfUtc,
            WriteOffTotal: roomCharges.WriteOffTotal);
    }

    public static IpdFinalBillDto ToFinalBillDto(IpdPayment payment, IpdBillCalculationResult roomCharges, bool wasCancelled = false) => new(
        payment.IpdRegistrationId,
        payment.InitialFee,
        payment.RoomChargesTotal,
        roomCharges.LineItems,
        payment.ParticularsTotal,
        payment.TotalAmount,
        payment.Discount,
        payment.FinalAmount,
        payment.TotalDeposited,
        payment.BalanceDue,
        payment.FinalizedAt!.Value,
        payment.PaymentStatus,
        payment.RefundDue,
        payment.RefundStatus,
        payment.SettledAt,
        payment.RefundedAt,
        roomCharges.WriteOffTotal,
        wasCancelled);
}
