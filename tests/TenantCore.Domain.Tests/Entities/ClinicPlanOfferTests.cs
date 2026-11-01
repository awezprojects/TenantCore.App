using FluentAssertions;
using TenantCore.Domain.Entities;

namespace TenantCore.Domain.Tests.Entities;

public class ClinicPlanOfferTests
{
    private static readonly DateTime Now = new(2026, 06, 15, 10, 0, 0, DateTimeKind.Utc);

    private static ClinicPlanOffer Offer(DateTime? validUntil = null, decimal? price = 499m) =>
        ClinicPlanOffer.Create(Guid.NewGuid(), Guid.NewGuid(), price, validUntil, "note", "admin@example.test");

    [Fact]
    public void IsLive_ActiveWithNoExpiry_ReturnsTrue()
        => Offer(validUntil: null).IsLive(Now).Should().BeTrue();

    [Fact]
    public void IsLive_ActiveAndNotYetExpired_ReturnsTrue()
        => Offer(validUntil: Now.AddDays(1)).IsLive(Now).Should().BeTrue();

    [Fact]
    public void IsLive_Expired_ReturnsFalse()
        => Offer(validUntil: Now.AddDays(-1)).IsLive(Now).Should().BeFalse();

    [Fact]
    public void IsLive_Withdrawn_ReturnsFalse()
    {
        var offer = Offer();
        offer.Withdraw("admin@example.test");

        offer.IsLive(Now).Should().BeFalse();
    }

    [Fact]
    public void Withdraw_RecordsAdminAndTime()
    {
        var offer = Offer();

        offer.Withdraw("admin@example.test");

        offer.IsActive.Should().BeFalse();
        offer.WithdrawnByAdminEmail.Should().Be("admin@example.test");
        offer.WithdrawnAt.Should().NotBeNull();
    }

    [Fact]
    public void Withdraw_AlreadyWithdrawn_Throws()
    {
        var offer = Offer();
        offer.Withdraw("admin@example.test");

        var act = () => offer.Withdraw("admin@example.test");

        act.Should().Throw<InvalidOperationException>().WithMessage("*already been withdrawn*");
    }
}
