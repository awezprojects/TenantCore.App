using FluentAssertions;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Tests.Entities;

public class ClinicAccountTests
{
    private static ClinicAccount NewAccount() => ClinicAccount.CreateDefault(Guid.NewGuid());

    [Fact]
    public void CreateDefault_IsActiveAndUnrestricted()
    {
        var account = NewAccount();

        account.AccessStatus.Should().Be(ClinicAccessStatus.Active);
        account.IsSuspended.Should().BeFalse();
        account.RestrictToOfferedPlans.Should().BeFalse();
    }

    [Fact]
    public void Suspend_RecordsMessageAndAdmin()
    {
        var account = NewAccount();

        account.Suspend("Payment overdue", "admin@example.test");

        account.IsSuspended.Should().BeTrue();
        account.SuspensionMessage.Should().Be("Payment overdue");
        account.SuspendedByAdminEmail.Should().Be("admin@example.test");
        account.SuspendedAt.Should().NotBeNull();
    }

    [Fact]
    public void Suspend_AlreadySuspended_Throws()
    {
        var account = NewAccount();
        account.Suspend("first", "admin@example.test");

        var act = () => account.Suspend("second", "admin@example.test");

        act.Should().Throw<InvalidOperationException>().WithMessage("*already suspended*");
    }

    [Fact]
    public void Reactivate_RestoresAccessAndKeepsTheLastMessage()
    {
        var account = NewAccount();
        account.Suspend("Payment overdue", "admin@example.test");

        account.Reactivate("other@example.test");

        account.IsSuspended.Should().BeFalse();
        account.ReactivatedByAdminEmail.Should().Be("other@example.test");
        account.SuspensionMessage.Should().Be("Payment overdue", "the admin portal shows what the last suspension said");
    }

    [Fact]
    public void Reactivate_NotSuspended_Throws()
    {
        var account = NewAccount();

        var act = () => account.Reactivate("admin@example.test");

        act.Should().Throw<InvalidOperationException>().WithMessage("*not suspended*");
    }

    [Fact]
    public void SetRestrictToOfferedPlans_TogglesTheFlag()
    {
        var account = NewAccount();

        account.SetRestrictToOfferedPlans(true);
        account.RestrictToOfferedPlans.Should().BeTrue();

        account.SetRestrictToOfferedPlans(false);
        account.RestrictToOfferedPlans.Should().BeFalse();
    }
}
