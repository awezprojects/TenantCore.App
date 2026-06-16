using FluentValidation;
using TenantCore.Application.Features.Applications.Queries;

namespace TenantCore.Application.Features.Applications.Validators;

public sealed class GetApplicationByCodeQueryValidator : AbstractValidator<GetApplicationByCodeQuery>
{
    public GetApplicationByCodeQueryValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
    }
}
