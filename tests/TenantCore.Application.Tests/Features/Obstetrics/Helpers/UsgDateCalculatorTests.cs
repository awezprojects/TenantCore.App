using FluentAssertions;
using TenantCore.Application.Features.Obstetrics.Helpers;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.Obstetrics.Helpers;

public class UsgDateCalculatorTests
{
    // 2024-01-01 is a Monday
    private static readonly DateOnly BaseLmp = new(2024, 1, 1);

    [Fact]
    public void CalculateSchedule_WhenTemplateRowsProvided_ReturnsRowsInOrder()
    {
        var rows = new List<UsgTemplateRowDto>
        {
            new() { RowOrder = 3, WeekLabel = "12 Weeks", LmpDayOffset = 84, Activity = "NT Scan",   Indication = "Nuchal translucency" },
            new() { RowOrder = 1, WeekLabel = "6 Weeks",  LmpDayOffset = 42, Activity = "Dating Scan", Indication = "Confirm viability" },
            new() { RowOrder = 2, WeekLabel = "10 Weeks", LmpDayOffset = 70, Activity = "Early Scan", Indication = "Growth check" },
        };

        var result = UsgDateCalculator.CalculateSchedule(BaseLmp, rows);

        result.Should().HaveCount(3);
        result[0].RowOrder.Should().Be(1);
        result[1].RowOrder.Should().Be(2);
        result[2].RowOrder.Should().Be(3);
    }

    [Fact]
    public void CalculateSchedule_WhenDateFallsOnSunday_ShiftsToMonday()
    {
        // BaseLmp = 2024-01-01 (Monday); +6 days = 2024-01-07 (Sunday)
        var rows = new List<UsgTemplateRowDto>
        {
            new() { RowOrder = 1, WeekLabel = "~1 Week", LmpDayOffset = 6, Activity = "Early check", Indication = "Test" },
        };

        var result = UsgDateCalculator.CalculateSchedule(BaseLmp, rows);

        result[0].Date.DayOfWeek.Should().NotBe(DayOfWeek.Sunday);
        result[0].Date.Should().Be(new DateOnly(2024, 1, 8)); // Monday
    }

    [Fact]
    public void CalculateSchedule_WhenDateDoesNotFallOnSunday_KeepsOriginalDate()
    {
        // BaseLmp = 2024-01-01 (Monday); +7 days = 2024-01-08 (Monday) — no shift
        var rows = new List<UsgTemplateRowDto>
        {
            new() { RowOrder = 1, WeekLabel = "1 Week", LmpDayOffset = 7, Activity = "One-week check", Indication = "Test" },
        };

        var result = UsgDateCalculator.CalculateSchedule(BaseLmp, rows);

        result[0].Date.Should().Be(new DateOnly(2024, 1, 8));
        result[0].Date.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public void CalculateSchedule_MapsAllRowFields()
    {
        var row = new UsgTemplateRowDto
        {
            RowOrder     = 4,
            WeekLabel    = "20 Weeks",
            LmpDayOffset = 140,
            Activity     = "Anomaly Scan",
            Indication   = "Detailed fetal anatomy",
        };

        var result = UsgDateCalculator.CalculateSchedule(BaseLmp, [row]);

        result[0].RowOrder.Should().Be(row.RowOrder);
        result[0].WeekLabel.Should().Be(row.WeekLabel);
        result[0].Activity.Should().Be(row.Activity);
        result[0].Indication.Should().Be(row.Indication);
        result[0].DayOfWeek.Should().Be(BaseLmp.AddDays(row.LmpDayOffset).DayOfWeek.ToString());
    }

    [Fact]
    public void CalculateSchedule_WhenNoRows_ReturnsEmptyList()
    {
        var result = UsgDateCalculator.CalculateSchedule(BaseLmp, []);

        result.Should().BeEmpty();
    }
}
