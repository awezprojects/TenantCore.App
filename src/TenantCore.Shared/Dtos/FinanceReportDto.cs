namespace TenantCore.Shared.Dtos;

public class FinanceDashboardSummaryDto
{
    public decimal TotalCollected { get; init; }
    public decimal TotalExpenses { get; init; }
    public decimal NetAmount { get; init; }
    public bool HasActiveSession { get; init; }
    public Guid? ActiveSessionId { get; init; }
    public DateTime? ActiveSessionOpenedAt { get; init; }
}

public class DailyCollectionReportDto
{
    public DateTime Date { get; init; }
    public IReadOnlyList<DailyCollectionLineItemDto> Items { get; init; } = [];
    public decimal GrandTotal { get; init; }
}

public class DailyCollectionLineItemDto
{
    public string PatientName { get; init; } = string.Empty;
    public decimal VisitFee { get; init; }
    public decimal ParticularsTotal { get; init; }
    public decimal Discount { get; init; }
    public decimal FinalAmount { get; init; }
}

public class WeeklyCollectionReportDto
{
    public DateTime WeekStart { get; init; }
    public IReadOnlyList<DailyTotalDto> DayTotals { get; init; } = [];
    public decimal GrandTotal { get; init; }
}

public class DailyTotalDto
{
    public DateTime Date { get; init; }
    public string DayOfWeek { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}

public class MonthlyCollectionReportDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public IReadOnlyList<DailyTotalDto> DayTotals { get; init; } = [];
    public IReadOnlyList<WeeklySubtotalDto> WeeklySubtotals { get; init; } = [];
    public decimal GrandTotal { get; init; }
}

public class WeeklySubtotalDto
{
    public int WeekNumber { get; init; }
    public decimal Amount { get; init; }
}

public class DateRangeCollectionReportDto
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public IReadOnlyList<DailyTotalDto> DayTotals { get; init; } = [];
    public decimal GrandTotal { get; init; }
}

public class ReceptionWiseReportDto
{
    public Guid UserId { get; init; }
    public int TotalSessions { get; init; }
    public decimal TotalCollected { get; init; }
    public decimal TotalExpenses { get; init; }
}

public class ExpenseSummaryReportDto
{
    public IReadOnlyList<ExpenseCategoryTotalDto> CategoryTotals { get; init; } = [];
    public decimal GrandTotal { get; init; }
}

public class ExpenseCategoryTotalDto
{
    public string CategoryName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
