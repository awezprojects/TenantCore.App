using FluentValidation;
using TenantCore.Application.Features.IpdReports.Queries;

namespace TenantCore.Application.Features.IpdReports.Validators;

public sealed class GetIpdDischargeSummaryReportQueryValidator : AbstractValidator<GetIpdDischargeSummaryReportQuery>
{
    public GetIpdDischargeSummaryReportQueryValidator()
    {
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.ToUtc).GreaterThan(x => x.FromUtc);
        RuleFor(x => x).Must(x => (x.ToUtc - x.FromUtc).TotalDays <= 366)
            .WithMessage("Date range cannot exceed one year.");
    }
}
