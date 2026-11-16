using FluentAssertions;
using TenantCore.Application.Features.IpdReports.Queries;
using TenantCore.Application.Features.IpdReports.Validators;

namespace TenantCore.Application.Tests.Features.IpdReports.Validators;

public class GetIpdRevenueReportQueryValidatorTests
{
    private readonly GetIpdRevenueReportQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidRange_PassesValidation()
        => _validator.Validate(new GetIpdRevenueReportQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-7), DateTime.UtcNow)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ToBeforeFrom_FailsWithError()
        => _validator.Validate(new GetIpdRevenueReportQuery(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddDays(-7))).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_RangeOverOneYear_FailsWithError()
        => _validator.Validate(new GetIpdRevenueReportQuery(Guid.NewGuid(), DateTime.UtcNow.AddDays(-400), DateTime.UtcNow)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
        => _validator.Validate(new GetIpdRevenueReportQuery(Guid.Empty, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow)).IsValid.Should().BeFalse();
}
