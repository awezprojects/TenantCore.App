using FluentValidation;
using TenantCore.Application.Common;
using TenantCore.Application.Features.PlatformAdmin.Commands;

namespace TenantCore.Application.Features.PlatformAdmin.Validators;

public sealed class SuspendClinicCommandValidator : AbstractValidator<SuspendClinicCommand>
{
    public SuspendClinicCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Message).ReasonRules("message");
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}

public sealed class ReactivateClinicCommandValidator : AbstractValidator<ReactivateClinicCommand>
{
    public ReactivateClinicCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.AdminUserId).AdminUserIdRules();
        RuleFor(x => x.AdminEmail).AdminEmailRules();
    }
}
