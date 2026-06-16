using FluentAssertions;
using Moq;
using TenantCore.Application.Common;
using TenantCore.Application.Features.Patients.Handlers;
using TenantCore.Application.Features.Patients.Queries;
using TenantCore.Domain.Entities;
using TenantCore.Domain.Exceptions;
using TenantCore.Domain.Interfaces;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Tests.Features.Patients.Handlers;

public class GetPatientByIdHandlerTests
{
    private readonly Mock<IPatientRepository> _repository = new();
    private readonly Mock<IApplicationAccessValidator> _accessValidator = new();

    [Fact]
    public async Task Handle_WhenPatientExistsAndAccessGranted_ReturnsDto()
    {
        var patient = CreatePatient();
        var query = new GetPatientByIdQuery(patient.Id, patient.ApplicationId, ShowFullAadhaar: true);

        _repository.Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _accessValidator.Setup(v => v.CanAccess(patient.ApplicationId))
            .Returns(true);

        var handler = new GetPatientByIdHandler(_repository.Object, _accessValidator.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Id.Should().Be(patient.Id);
        result.ApplicationId.Should().Be(patient.ApplicationId);
        result.FirstName.Should().Be(patient.FirstName);
        result.LastName.Should().Be(patient.LastName);
        result.AadhaarNumber.Should().Be(patient.AadhaarNumber);
    }

    [Fact]
    public async Task Handle_WhenPatientNotFound_ThrowsNotFoundException()
    {
        var query = new GetPatientByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        _repository.Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patient?)null);

        var handler = new GetPatientByIdHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAccessDenied_ThrowsUnauthorizedAccessException()
    {
        var patient = CreatePatient();
        var query = new GetPatientByIdQuery(patient.Id, patient.ApplicationId);

        _repository.Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patient);
        _accessValidator.Setup(v => v.CanAccess(patient.ApplicationId))
            .Returns(false);

        var handler = new GetPatientByIdHandler(_repository.Object, _accessValidator.Object);

        Func<Task> action = () => handler.Handle(query, CancellationToken.None);

        await action.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Access denied.");
    }

    private static Patient CreatePatient(Guid? applicationId = null)
        => Patient.Create(
            applicationId ?? Guid.NewGuid(),
            "Jane",
            "Doe",
            new DateOnly(1995, 4, 20),
            Gender.Female,
            "+1234567890",
            "jane@example.com",
            "123456789012",
            "https://cdn.example.com/patient.jpg",
            "123 Main St",
            "O+",
            "John Doe",
            "+1987654321",
            "Peanuts",
            "Asthma");
}
