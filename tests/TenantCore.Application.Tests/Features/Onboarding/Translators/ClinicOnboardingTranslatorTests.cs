using FluentAssertions;
using TenantCore.Application.Features.Onboarding.Translators;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Onboarding.Translators;

public class ClinicOnboardingTranslatorTests
{
    private static ClinicOnboardingRequest CreateRequest() => ClinicOnboardingRequest.Submit(
        Guid.NewGuid(), "Requester", "requester@example.test", "9876543210", "Sunrise Clinic", "SUNRISE",
        "123 Main St", "Pune", "MH", "411001", "0201234567", "office@sunrise.test", "https://sunrise.test",
        "Dr. Jane", "MR12345", "MCI", 5, "Google", "Some notes");

    [Fact]
    public void ToDto_MapsAllUserFacingFields()
    {
        var request = CreateRequest();

        var dto = ClinicOnboardingTranslator.ToDto(request, null);

        dto.Id.Should().Be(request.Id);
        dto.ClinicName.Should().Be("Sunrise Clinic");
        dto.PreferredClinicCode.Should().Be("SUNRISE");
        dto.Address.Should().Be("123 Main St");
        dto.City.Should().Be("Pune");
        dto.State.Should().Be("MH");
        dto.Pincode.Should().Be("411001");
        dto.ClinicContactNumber.Should().Be("0201234567");
        dto.OfficialEmail.Should().Be("office@sunrise.test");
        dto.Website.Should().Be("https://sunrise.test");
        dto.DoctorName.Should().Be("Dr. Jane");
        dto.MedicalRegistrationNumber.Should().Be("MR12345");
        dto.MedicalCouncil.Should().Be("MCI");
        dto.ExpectedStaffCount.Should().Be(5);
        dto.Notes.Should().Be("Some notes");
        dto.Status.Should().Be(ClinicOnboardingStatus.Submitted);
        dto.StatusText.Should().Be("Pending review");
        dto.CreatedAt.Should().Be(request.CreatedAt);
    }

    [Fact]
    public void ToDto_AwaitingPaymentStatus_IncludesPaymentLinkUrlAndExpiry()
    {
        var request = CreateRequest();
        request.ApproveWithPaidPlan(Guid.NewGuid(), SubscriptionPlanCode.Monthly, 999m, 999m, null, null, null, Guid.NewGuid(), "admin@example.test");
        var payment = SubscriptionPayment.CreateForOnboarding(request.Id, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, 999m, "INR", "Dr", "dr@example.test", "9876543210");
        var expiry = DateTime.UtcNow.AddDays(7);
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", expiry);
        request.SetCurrentPayment(payment.Id);
        request.MarkAwaitingPayment();

        var dto = ClinicOnboardingTranslator.ToDto(request, payment);

        dto.PaymentLinkUrl.Should().Be("https://razorpay.test/pay/plink_1");
        dto.LinkExpiresAt.Should().Be(expiry);
    }

    [Fact]
    public void ToDto_NonAwaitingPaymentStatus_ExcludesPaymentLinkUrlEvenIfPaymentPassed()
    {
        var request = CreateRequest(); // Submitted
        var payment = SubscriptionPayment.CreateForOnboarding(request.Id, Guid.NewGuid(), SubscriptionPlanCode.Monthly, "Monthly", 999m, 999m, "INR", "Dr", "dr@example.test", "9876543210");
        payment.SetLink("plink_1", "https://razorpay.test/pay/plink_1", DateTime.UtcNow.AddDays(7));

        var dto = ClinicOnboardingTranslator.ToDto(request, payment);

        dto.PaymentLinkUrl.Should().BeNull();
        dto.LinkExpiresAt.Should().BeNull();
    }

    [Theory]
    [InlineData(ClinicOnboardingStatus.Submitted, "Pending review")]
    [InlineData(ClinicOnboardingStatus.Approved, "Approved — preparing payment")]
    [InlineData(ClinicOnboardingStatus.AwaitingPayment, "Awaiting payment")]
    [InlineData(ClinicOnboardingStatus.PaymentReceived, "Payment received — setting up")]
    [InlineData(ClinicOnboardingStatus.Provisioning, "Setting up your clinic")]
    [InlineData(ClinicOnboardingStatus.Active, "Active")]
    [InlineData(ClinicOnboardingStatus.Rejected, "Not approved")]
    [InlineData(ClinicOnboardingStatus.Cancelled, "Cancelled")]
    [InlineData(ClinicOnboardingStatus.PaymentLinkExpired, "Payment link expired")]
    public void ToStatusText_EveryStatus_ReturnsExpectedFriendlyText(ClinicOnboardingStatus status, string expected)
    {
        ClinicOnboardingTranslator.ToStatusText(status).Should().Be(expected);
    }
}
