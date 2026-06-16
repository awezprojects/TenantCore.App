using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Features.Obstetrics.Helpers;

public static class UsgDateCalculator
{
    public static IReadOnlyList<UsgChartRowDto> CalculateSchedule(DateOnly lmp, IEnumerable<UsgTemplateRowDto> templateRows)
    {
        return templateRows
            .OrderBy(r => r.RowOrder)
            .Select(row =>
            {
                var date = AdjustForSunday(lmp.AddDays(row.LmpDayOffset));
                return new UsgChartRowDto
                {
                    RowOrder   = row.RowOrder,
                    WeekLabel  = row.WeekLabel,
                    Date       = date,
                    DayOfWeek  = date.DayOfWeek.ToString(),
                    Activity   = row.Activity,
                    Indication = row.Indication,
                };
            })
            .ToList()
            .AsReadOnly();
    }

    private static DateOnly AdjustForSunday(DateOnly date)
        => date.DayOfWeek == System.DayOfWeek.Sunday ? date.AddDays(1) : date;
}
