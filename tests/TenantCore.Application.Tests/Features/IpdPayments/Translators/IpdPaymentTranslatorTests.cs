using FluentAssertions;
using TenantCore.Application.Features.IpdPayments.Services;
using TenantCore.Application.Features.IpdPayments.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdPayments.Translators;

public class IpdPaymentTranslatorTests
{
    private readonly Guid _appId = Guid.NewGuid();

    [Fact]
    public void ToProvisionalBillDto_MapsAllFieldsAndFlagsProvisionalTrue()
    {
        var registration = IpdRegistration.Create(_appId, Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        var payment = IpdPayment.Create(_appId, registration.Id, 2000m);
        payment.IncreaseParticularsTotal(300m);
        payment.IncreaseDeposited(1000m);
        payment.ApplyDiscount(200m, Guid.NewGuid());
        var roomCharges = new IpdBillCalculationResult(500m, [], 0);
        var asOfUtc = DateTime.UtcNow;

        var dto = IpdPaymentTranslator.ToProvisionalBillDto(registration, payment, roomCharges, asOfUtc);

        dto.IsProvisional.Should().BeTrue();
        dto.InitialFee.Should().Be(2000m);
        dto.RoomChargesTotal.Should().Be(500m);
        dto.ParticularsTotal.Should().Be(300m);
        dto.TotalAmount.Should().Be(2800m);
        dto.Discount.Should().Be(200m);
        dto.FinalAmount.Should().Be(2600m);
        dto.TotalDeposited.Should().Be(1000m);
        dto.BalanceDue.Should().Be(1600m);
        dto.AsOfUtc.Should().Be(asOfUtc);
    }

    [Fact]
    public void ToFinalBillDto_MapsLockedFields()
    {
        var payment = IpdPayment.Create(_appId, Guid.NewGuid(), 2000m);
        payment.FinalizeAtDischarge(500m, DateTime.UtcNow);
        var roomCharges = new IpdBillCalculationResult(500m, [], 0);

        var dto = IpdPaymentTranslator.ToFinalBillDto(payment, roomCharges);

        dto.FinalAmount.Should().Be(2500m);
        dto.FinalizedAt.Should().Be(payment.FinalizedAt!.Value);
        dto.PaymentStatus.Should().Be(PaymentStatus.Pending);
        dto.WasCancelled.Should().BeFalse("the default, for every pre-existing call site that doesn't pass it");
    }

    [Fact]
    public void ToFinalBillDto_WasCancelledTrue_IsPassedThrough()
    {
        // DEF-IPD-22 (14-Nov QA).
        var payment = IpdPayment.Create(_appId, Guid.NewGuid(), 100m);
        payment.FinalizeAtCancellation(DateTime.UtcNow);
        var roomCharges = new IpdBillCalculationResult(0m, [], 0);

        var dto = IpdPaymentTranslator.ToFinalBillDto(payment, roomCharges, wasCancelled: true);

        dto.WasCancelled.Should().BeTrue();
    }
}
