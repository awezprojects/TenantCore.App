using MediatR;
using TenantCore.Application.Features.FinanceReports.Queries;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.FinanceReports.Handlers;

public sealed class GetExpenseSummaryReportHandler(IExpenseRecordRepository repository) : IRequestHandler<GetExpenseSummaryReportQuery, ExpenseSummaryReportDto>
{
    public async Task<ExpenseSummaryReportDto> Handle(GetExpenseSummaryReportQuery request, CancellationToken cancellationToken)
    {
        var offset = request.UtcOffsetMinutes;
        var fromUtc = request.From.Date.AddMinutes(-offset);
        var toUtc   = request.To.Date.AddDays(1).AddMinutes(-offset);
        var records = await repository.GetByDateRangeAsync(fromUtc, toUtc, request.ApplicationId, cancellationToken);

        var categoryTotals = records
            .GroupBy(r => r.CategoryName)
            .Select(g => new ExpenseCategoryTotalDto { CategoryName = g.Key, Amount = g.Sum(r => r.Amount) })
            .OrderByDescending(c => c.Amount)
            .ToList();

        return new ExpenseSummaryReportDto
        {
            CategoryTotals = categoryTotals,
            GrandTotal = categoryTotals.Sum(c => c.Amount)
        };
    }
}
