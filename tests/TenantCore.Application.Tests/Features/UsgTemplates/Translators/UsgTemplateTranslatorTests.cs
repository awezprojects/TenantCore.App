using FluentAssertions;
using TenantCore.Application.Features.UsgTemplates.Translators;
using TenantCore.Application.Features.Obstetrics.Helpers;
using TenantCore.Domain.Entities;
using TenantCore.Shared.Dtos;

namespace TenantCore.Application.Tests.Features.UsgTemplates.Translators;

public class UsgTemplateTranslatorTests
{
    [Fact]
    public void ToDto_WhenTemplateWithRows_MapsAllProperties()
    {
        var applicationId = Guid.NewGuid();
        var template = new ClinicUsgTemplate
        {
            ApplicationId = applicationId,
            IsCustomized  = true,
            Rows =
            [
                new UsgTemplateRow { RowOrder = 2, WeekLabel = "10 Weeks", LmpDayOffset = 70, Activity = "Early Scan", Indication = "Growth check" },
                new UsgTemplateRow { RowOrder = 1, WeekLabel = "6 Weeks",  LmpDayOffset = 42, Activity = "Dating Scan", Indication = "Confirm viability" },
            ],
        };

        var result = UsgTemplateTranslator.ToDto(template);

        result.ApplicationId.Should().Be(applicationId);
        result.IsCustomized.Should().BeTrue();
        result.Rows.Should().HaveCount(2);
        result.Rows[0].RowOrder.Should().Be(1);
        result.Rows[1].RowOrder.Should().Be(2);
    }

    [Fact]
    public void RowToDto_WhenRowProvided_MapsAllFields()
    {
        var row = new UsgTemplateRow
        {
            RowOrder     = 3,
            WeekLabel    = "12 Weeks",
            LmpDayOffset = 84,
            Activity     = "NT Scan",
            Indication   = "Nuchal translucency screening",
        };

        var result = UsgTemplateTranslator.RowToDto(row);

        result.RowOrder.Should().Be(row.RowOrder);
        result.WeekLabel.Should().Be(row.WeekLabel);
        result.LmpDayOffset.Should().Be(row.LmpDayOffset);
        result.Activity.Should().Be(row.Activity);
        result.Indication.Should().Be(row.Indication);
    }

    [Fact]
    public void RowFromDto_WhenDtoProvided_MapsAllFields()
    {
        var templateId = Guid.NewGuid();
        var dto = new UsgTemplateRowDto
        {
            RowOrder     = 5,
            WeekLabel    = "16 Weeks",
            LmpDayOffset = 112,
            Activity     = "Mid-Trimester Screening",
            Indication   = "Quadruple screening",
        };

        var result = UsgTemplateTranslator.RowFromDto(dto, templateId);

        result.ClinicUsgTemplateId.Should().Be(templateId);
        result.RowOrder.Should().Be(dto.RowOrder);
        result.WeekLabel.Should().Be(dto.WeekLabel);
        result.LmpDayOffset.Should().Be(dto.LmpDayOffset);
        result.Activity.Should().Be(dto.Activity);
        result.Indication.Should().Be(dto.Indication);
    }

    [Fact]
    public void ToDefaultDto_WhenApplicationIdProvided_ReturnsDefaultTemplate()
    {
        var applicationId = Guid.NewGuid();

        var result = UsgTemplateTranslator.ToDefaultDto(applicationId);

        result.ApplicationId.Should().Be(applicationId);
        result.IsCustomized.Should().BeFalse();
        result.Rows.Should().NotBeEmpty();
        result.Rows.Should().BeEquivalentTo(DefaultUsgTemplateDefinition.Rows);
    }
}
