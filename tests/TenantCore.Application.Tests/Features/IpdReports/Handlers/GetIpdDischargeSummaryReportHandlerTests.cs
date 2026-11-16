using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.IpdReports.Handlers;
using TenantCore.Application.Features.IpdReports.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.IpdReports.Handlers;

public class GetIpdDischargeSummaryReportHandlerTests
{
    private readonly Mock<IIpdRegistrationRepository> _ipdRepository = new();
    private readonly Mock<IIpdPaymentRepository> _paymentRepository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();
    private readonly Guid _appId = Guid.NewGuid();
    private readonly DateTime _from = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly DateTime _to = new(2100, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private GetIpdDischargeSummaryReportHandler CreateHandler() => new(_ipdRepository.Object, _paymentRepository.Object, _accessValidator.Object);

    [Fact]
    public async Task Handle_DischargedRegistrationsWithPayments_IncludesPatientAndBillDetails()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var patient = Patient.Create(_appId, "Jane", "Doe", null, Gender.Female, "9876543210", null, null, null, null);
        var registration = IpdRegistration.Create(_appId, patient.Id, Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        registration.Discharge(null);
        typeof(IpdRegistration).GetProperty(nameof(IpdRegistration.Patient))!.SetValue(registration, patient);

        var payment = IpdPayment.Create(_appId, registration.Id, 2000m);
        payment.FinalizeAtDischarge(0, DateTime.UtcNow);
        payment.CollectBalance(Guid.NewGuid(), null);

        _ipdRepository.Setup(r => r.GetDischargesByDateRangeAsync(_from, _to, _appId, It.IsAny<CancellationToken>())).ReturnsAsync([registration]);
        _paymentRepository.Setup(r => r.GetByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await CreateHandler().Handle(new GetIpdDischargeSummaryReportQuery(_appId, _from, _to), CancellationToken.None);

        result.Discharges.Should().ContainSingle();
        var row = result.Discharges[0];
        row.PatientName.Should().Be("Jane Doe");
        row.PatientPhoneNumber.Should().Be("9876543210");
        row.FinalAmount.Should().Be(2000m);
        row.Settled.Should().BeTrue();
        row.HasBill.Should().BeTrue();
        // OBS-7: BalanceDue is a locked snapshot that CollectBalance never zeros, but a settled
        // row must display 0 here — not the stale raw figure that still reads as "owed".
        row.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_RefundedRegistration_DisplaysZeroBalanceOnceSettled()
    {
        // OBS-7 regression: a refunded admission's raw BalanceDue stays negative forever by
        // design — this report must not surface that as a still-outstanding amount.
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var patient = Patient.Create(_appId, "Jane", "Doe", null, Gender.Female, "9876543210", null, null, null, null);
        var registration = IpdRegistration.Create(_appId, patient.Id, Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 500m, null);
        registration.Discharge(null);
        typeof(IpdRegistration).GetProperty(nameof(IpdRegistration.Patient))!.SetValue(registration, patient);

        var payment = IpdPayment.Create(_appId, registration.Id, 500m);
        payment.IncreaseDeposited(1000m);
        payment.FinalizeAtDischarge(0, DateTime.UtcNow); // BalanceDue = 500 - 1000 = -500 → refund due
        payment.ProcessRefund(Guid.NewGuid());

        _ipdRepository.Setup(r => r.GetDischargesByDateRangeAsync(_from, _to, _appId, It.IsAny<CancellationToken>())).ReturnsAsync([registration]);
        _paymentRepository.Setup(r => r.GetByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([payment]);

        var result = await CreateHandler().Handle(new GetIpdDischargeSummaryReportQuery(_appId, _from, _to), CancellationToken.None);

        var row = result.Discharges[0];
        row.Settled.Should().BeTrue();
        row.BalanceDue.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_NoPaymentRecord_SettledFalseAndNullAmounts()
    {
        _accessValidator.Setup(v => v.CanAccess(_appId)).Returns(true);
        var patient = Patient.Create(_appId, "Jane", "Doe", null, Gender.Female, "9876543210", null, null, null, null);
        var registration = IpdRegistration.Create(_appId, patient.Id, Guid.NewGuid(), "Dr. Smith", "IPD-1", null, null, null, null, 2000m, null);
        registration.Discharge(null);
        typeof(IpdRegistration).GetProperty(nameof(IpdRegistration.Patient))!.SetValue(registration, patient);

        _ipdRepository.Setup(r => r.GetDischargesByDateRangeAsync(_from, _to, _appId, It.IsAny<CancellationToken>())).ReturnsAsync([registration]);
        _paymentRepository.Setup(r => r.GetByRegistrationIdsAsync(It.IsAny<IEnumerable<Guid>>(), _appId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateHandler().Handle(new GetIpdDischargeSummaryReportQuery(_appId, _from, _to), CancellationToken.None);

        result.Discharges[0].FinalAmount.Should().BeNull();
        result.Discharges[0].BalanceDue.Should().BeNull();
        result.Discharges[0].Settled.Should().BeFalse();
        // OBS-8: no IpdPayment row at all (pre-feature discharge) must read as "no bill", not
        // as an outstanding one — the UI uses this flag, not Settled, to pick that label.
        result.Discharges[0].HasBill.Should().BeFalse();
    }
}
