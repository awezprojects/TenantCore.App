using FluentAssertions;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Validators;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Prescriptions.Validators;

public class UpdatePrescriptionCommandValidatorTests
{
    private readonly UpdatePrescriptionCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ReturnsValid()
    {
        var command = CreateCommand();

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
    public void Validate_WhenVitalSugarIsNegative_ReturnsError()
    {
        var command = CreateCommand(vitalSugar: -1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("VitalSugar"));
    }

    [Fact]
    public void Validate_WhenObstetricAbortionIsNegative_ReturnsError()
    {
        var command = CreateCommand(obstetricData: new UpsertObstetricPrescriptionDataDto(
            Gravida: 1, Para: 0, Live: 0, Abortion: -1, Information: null,
            MenstrualHistory: null, PastMedicalHistory: null, FamilyHistory: null));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName.Contains("Abortion"));
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ReturnsError()
    {
        var command = CreateCommand(id: Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == "Id");
    }

    private static UpdatePrescriptionCommand CreateCommand(
        Guid? id = null,
        DateTime? nextVisitDate = null,
        decimal? vitalSugar = null,
        UpsertObstetricPrescriptionDataDto? obstetricData = null)
        => new(
            id ?? Guid.NewGuid(),
            Guid.NewGuid(),
            nextVisitDate,
            "Diagnosis",
            [],
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            vitalSugar,
            [DefaultItem()],
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
