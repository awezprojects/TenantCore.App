using FluentValidation;
using TenantCore.Application.Features.HistoryLookupItems.Commands;

namespace TenantCore.Application.Features.HistoryLookupItems.Validators;

public sealed class AddHistoryLookupItemCommandValidator : AbstractValidator<AddHistoryLookupItemCommand>
{
    public AddHistoryLookupItemCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Value)
            .Must(v => v.Trim().Length is >= 3 and <= 256)
            .WithMessage("Value must be between 3 and 256 characters.");
    }
}
