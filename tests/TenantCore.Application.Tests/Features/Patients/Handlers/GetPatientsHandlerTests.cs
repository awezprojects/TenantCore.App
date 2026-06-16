using FluentAssertions;
using Moq;
using TenantCore.Application.Features.Patients.Handlers;
using TenantCore.Application.Features.Patients.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Patients.Handlers;

public class GetPatientsHandlerTests
{
    private readonly Mock<IPatientRepository> _repository = new();
    private readonly Mock<IPregnancyTenureRepository> _tenureRepository = new();
    private readonly Mock<IObstetricPrescriptionDataRepository> _obstetricRepository = new();

    [Fact]
    public async Task Handle_WhenCalled_ReturnsPagedResult()
    {
        var applicationId = Guid.NewGuid();
        var firstPatient = CreatePatient(applicationId, "Jane", "Doe", "123456789012");
        var secondPatient = CreatePatient(applicationId, "John", "Smith", "987654321098");
        var query = new GetPatientsQuery(applicationId, Page: 2, PageSize: 10, Search: "Jo", ShowFullAadhaar: false);

        _repository.Setup(r => r.GetPagedAsync(applicationId, query.Page, query.PageSize, query.Search, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new[] { firstPatient, secondPatient }, 2));
        _tenureRepository.Setup(r => r.GetTenureInfoForPatientsAsync(
                It.IsAny<IEnumerable<Guid>>(), applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, bool>
            {
                [firstPatient.Id] = true
            });
        _obstetricRepository.Setup(r => r.GetPatientIdsWithLmpAsync(
                It.IsAny<IEnumerable<Guid>>(), applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { secondPatient.Id });

        var handler = new GetPatientsHandler(_repository.Object, _tenureRepository.Object, _obstetricRepository.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.Items.Should().HaveCount(2);

        result.Items[0].Id.Should().Be(firstPatient.Id);
        result.Items[0].AadhaarNumber.Should().Be("XXXX-XXXX-9012");
        result.Items[0].HasLmpRecord.Should().BeTrue();
        result.Items[0].HasActiveTenure.Should().BeTrue();

        result.Items[1].Id.Should().Be(secondPatient.Id);
        result.Items[1].AadhaarNumber.Should().Be("XXXX-XXXX-1098");
        result.Items[1].HasLmpRecord.Should().BeTrue();
        result.Items[1].HasActiveTenure.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNoPatients_ReturnsEmptyPagedResult()
    {
        var applicationId = Guid.NewGuid();
        var query = new GetPatientsQuery(applicationId, Page: 1, PageSize: 20);

        _repository.Setup(r => r.GetPagedAsync(applicationId, query.Page, query.PageSize, query.Search, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enumerable.Empty<Patient>(), 0));

        var handler = new GetPatientsHandler(_repository.Object, _tenureRepository.Object, _obstetricRepository.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        _tenureRepository.Verify(r => r.GetTenureInfoForPatientsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _obstetricRepository.Verify(r => r.GetPatientIdsWithLmpAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Patient CreatePatient(Guid applicationId, string firstName, string lastName, string aadhaarNumber)
        => Patient.Create(
            applicationId,
            firstName,
            lastName,
            new DateOnly(1995, 4, 20),
            Gender.Female,
            "+1234567890",
            $"{firstName.ToLowerInvariant()}@example.com",
            aadhaarNumber,
            null,
            "123 Main St",
            "O+");
}
