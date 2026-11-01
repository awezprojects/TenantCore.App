using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Clinics.Commands;
using TenantCore.Application.Features.Clinics.Handlers;
using TenantCore.Application.Services;
using TenantCore.Shared.Dtos.Auth;

namespace TenantCore.Application.Tests.Features.Clinics.Handlers;

public class CreateClinicHandlerTests
{
    private readonly Mock<IAuthClinicService> _clinicService = new();

    private static IOptions<OnboardingOptions> OptionsFor(string mode) =>
        Microsoft.Extensions.Options.Options.Create(new OnboardingOptions { Mode = mode });

    private static CreateClinicCommand CreateCommand() => new(new CreateClinicRequestDto
    {
        ClinicName = "Sunrise Clinic",
        ClinicCode = "SUNRISE"
    });

    [Fact]
    public async Task Handle_ApprovalMode_ThrowsInvalidOperationExceptionAndNeverCallsAuth()
    {
        var handler = new CreateClinicHandler(_clinicService.Object, OptionsFor("Approval"));

        var action = () => handler.Handle(CreateCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        _clinicService.Verify(s => s.CreateClinicAsync(It.IsAny<CreateClinicRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ApprovalModeIsCaseInsensitive_StillThrows()
    {
        var handler = new CreateClinicHandler(_clinicService.Object, OptionsFor("approval"));

        var action = () => handler.Handle(CreateCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_SelfServeMode_DelegatesToAuthClinicService()
    {
        var expected = new ApplicationResponseDto { ApplicationId = Guid.NewGuid(), ApplicationName = "Sunrise Clinic" };
        _clinicService.Setup(s => s.CreateClinicAsync(It.IsAny<CreateClinicRequestDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        var handler = new CreateClinicHandler(_clinicService.Object, OptionsFor("SelfServe"));
        var result = await handler.Handle(CreateCommand(), CancellationToken.None);

        result.Should().Be(expected);
        _clinicService.Verify(s => s.CreateClinicAsync(It.IsAny<CreateClinicRequestDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
