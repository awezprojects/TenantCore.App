using TenantCore.Application.Features.MedicineTypes.Commands;

namespace TenantCore.Application.Tests.TestData;

internal static class ApplicationTestData
{
    public const string MedicineTypeName = "Tablet";
    public const string MedicineTypeDescription = "Oral solid dosage form";
    public const string SecondMedicineTypeName = "Capsule";
    public const string SecondMedicineTypeDescription = "Second dosage form";
    public const string ExistingMedicineTypeName = "Existing Type";

    public static string DescriptionOverMaxLength => new('x', 501);

    public static CreateMedicineTypeCommand CreateMedicineTypeCommand(
        string? name = null,
        string? description = MedicineTypeDescription)
        => new(name ?? MedicineTypeName, description);
}
