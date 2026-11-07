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

    // DEF-MR-06: items=[] used to be rejected unconditionally — a counselling-only, scan-only or
    // follow-up visit could never be finalised at all. It's only an error when there is also no
    // other clinical content (diagnosis, investigations or notes) to justify the visit.
    [Fact]
    public void Validate_WhenItemsIsEmptyAndNoOtherClinicalContent_ReturnsError()
    {
        var command = CreateCommand(items: [], diagnosis: null, investigations: [], notes: null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.ErrorMessage.Contains("Add at least one medicine"));
    }

    [Fact]
    public void Validate_WhenItemsIsEmptyButDiagnosisIsPresent_ReturnsValid()
    {
        var command = CreateCommand(items: [], diagnosis: "Common cold — counselled, no medicine needed");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenItemsIsEmptyButInvestigationsOrdered_ReturnsValid()
    {
        var command = CreateCommand(items: [], diagnosis: null, investigations: ["USG Pelvic"]);

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenItemsIsEmptyButNotesPresent_ReturnsValid()
    {
        var command = CreateCommand(items: [], diagnosis: null, investigations: [], notes: "Follow-up visit, stable");

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    private static CreatePrescriptionCommand CreateCommand(
        DateTime? nextVisitDate = null,
        int? vitalPulse = null,
        UpsertObstetricPrescriptionDataDto? obstetricData = null,
        IReadOnlyList<CreatePrescriptionItemDto>? items = null,
        string? diagnosis = "Diagnosis",
        IReadOnlyList<string>? investigations = null,
        string? notes = null)
        => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Dr. Smith",
            nextVisitDate,
            diagnosis,
            investigations ?? [],
            notes,
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
