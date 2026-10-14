using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

/// <summary>
/// One admin-set price per billable operation per clinic. Setting a rate is a deactivate-and-insert
/// upsert done by the handler; here we hold the row's own rules — 0 is legal (free), negative is not.
/// </summary>
public class ClinicUsageRateTests
{
    private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ClinicUsageRate NewRate(decimal rate = 25m, BillingItem item = BillingItem.OpdRegistration)
        => ClinicUsageRate.Create(Guid.NewGuid(), item, rate, Now, "admin@example.test");

    [Fact]
    public void Create_MakesTheRowTheLiveOne()
    {
        var applicationId = Guid.NewGuid();

        var rate = ClinicUsageRate.Create(
            applicationId, BillingItem.IpdAdmission, 120.5m, Now, "admin@example.test");

        rate.ApplicationId.Should().Be(applicationId);
        rate.BillingItem.Should().Be(BillingItem.IpdAdmission);
        rate.Rate.Should().Be(120.5m);
        rate.IsActive.Should().BeTrue();
        rate.EffectiveFrom.Should().Be(Now);
        rate.UpdatedByAdminEmail.Should().Be("admin@example.test");
    }

    [Fact]
    public void Create_WithZero_PricesTheItemAsFree()
    {
        var rate = NewRate(0m);

        rate.Rate.Should().Be(0m, "zero means free, which is a legitimate thing to set");
        rate.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithANegativeRate_Throws()
    {
        var act = () => NewRate(-0.01m);

        act.Should().Throw<DomainException>().WithMessage("*cannot be negative*");
    }

    [Fact]
    public void Create_KeepsFourDecimalPrecision()
    {
        var rate = NewRate(12.3456m);

        rate.Rate.Should().Be(12.3456m);
    }

    [Fact]
    public void Deactivate_RetiresTheRowBeforeItsReplacementIsInserted()
    {
        var rate = NewRate();

        rate.Deactivate();

        rate.IsActive.Should().BeFalse();
        rate.Rate.Should().Be(25m, "the row is history now — its price is never rewritten");
    }

    [Fact]
    public void Deactivate_Twice_IsANoOp()
    {
        var rate = NewRate();
        rate.Deactivate();

        var act = () => rate.Deactivate();

        act.Should().NotThrow();
        rate.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateRate_CorrectsThePriceAndRecordsTheAdmin()
    {
        var rate = NewRate();

        rate.UpdateRate(99.9999m, "other@example.test");

        rate.Rate.Should().Be(99.9999m);
        rate.UpdatedByAdminEmail.Should().Be("other@example.test");
        rate.IsActive.Should().BeTrue("correcting a typo is not retiring the row");
    }

    [Fact]
    public void UpdateRate_WithANegativeRate_Throws()
    {
        var rate = NewRate();

        var act = () => rate.UpdateRate(-1m, "admin@example.test");

        act.Should().Throw<DomainException>().WithMessage("*cannot be negative*");
        rate.Rate.Should().Be(25m);
    }
}
