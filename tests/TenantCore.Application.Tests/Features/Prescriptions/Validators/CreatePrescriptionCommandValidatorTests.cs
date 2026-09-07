using FluentAssertions;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Validators;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Prescriptions.Validators;

public class CreatePrescriptionCommandValidatorTests
{
    private readonly CreatePrescriptionCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = CreateCommand();

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNextVisitDateIsExactlyNinetyDaysAhead_ReturnsValid()
    {
        var command = CreateCommand(nextVisitDate: DateTime.UtcNow.AddDays(90));

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNextVisitDateIsMoreThanNinetyDaysAhead_ReturnsError()
    {
        var command = CreateCommand(nextVisitDate: DateTime.UtcNow.AddDays(91));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("NextVisitDate"));
    }

    [Fact]
    public void Validate_WhenNextVisitDateIsInPast_ReturnsError()
    {
        var command = CreateCommand(nextVisitDate: DateTime.UtcNow.AddDays(-1));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("NextVisitDate"));
    }

    [Fact]
    public void Validate_WhenVitalPulseIsNegative_ReturnsError()
    {
        var command = CreateCommand(vitalPulse: -1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("VitalPulse"));
    }

    [Fact]
    public void Validate_WhenObstetricGravidaIsNegative_ReturnsError()
    {
        var command = CreateCommand(obstetricData: new UpsertObstetricPrescriptionDataDto(
            Gravida: -1, Para: null, Live: null, Abortion: null, Information: null,
            MenstrualHistory: null, PastMedicalHistory: null, FamilyHistory: null));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Gravida"));
    }

    [Fact]
    public void Validate_WhenObstetricLmpIsExactlyTenMonthsAgo_ReturnsValid()
    {
        var command = CreateCommand(obstetricData: new UpsertObstetricPrescriptionDataDto(
            Gravida: 1, Para: 0, Live: 0, Abortion: 0, Information: null,
            MenstrualHistory: null, PastMedicalHistory: null, FamilyHistory: null,
            Lmp: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-10)));

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenObstetricLmpIsMoreThanTenMonthsAgo_ReturnsError()
    {
        var command = CreateCommand(obstetricData: new UpsertObstetricPrescriptionDataDto(
            Gravida: 1, Para: 0, Live: 0, Abortion: 0, Information: null,
            MenstrualHistory: null, PastMedicalHistory: null, FamilyHistory: null,
            Lmp: DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-10).AddDays(-1)));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Lmp"));
    }

    [Fact]
    public void Validate_WhenObstetricLmpIsInFuture_ReturnsError()
    {
        var command = CreateCommand(obstetricData: new UpsertObstetricPrescriptionDataDto(
            Gravida: 1, Para: 0, Live: 0, Abortion: 0, Information: null,
            MenstrualHistory: null, PastMedicalHistory: null, FamilyHistory: null,
            Lmp: DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1)));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Lmp"));
    }

    [Fact]
    public void Validate_WhenItemsIsEmpty_ReturnsError()
    {
        var command = CreateCommand(items: []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("At least one medicine is required"));
    }

    private static CreatePrescriptionCommand CreateCommand(
        DateTime? nextVisitDate = null,
        int? vitalPulse = null,
        UpsertObstetricPrescriptionDataDto? obstetricData = null,
        IReadOnlyList<CreatePrescriptionItemDto>? items = null)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Dr. Smith",
            nextVisitDate,
            "Diagnosis",
            [],
            null,
            null,
            vitalPulse,
            null,
            null,
            null,
            null,
            null,
            items ?? [DefaultItem()],
            obstetricData);

    private static CreatePrescriptionItemDto DefaultItem() => new(
        Guid.NewGuid(),
        "Paracetamol",
        MedicineFormType.Tab,
        "500mg",
        "tablet",
        1, 1, 1, 1,
        5,
        1,
        "OD",
        "After food",
        null,
        null, null, null,
        1);
}
