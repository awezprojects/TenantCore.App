using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.Patients.Commands;
using TenantCore.Application.Features.Patients.Queries;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Controllers;

public class PatientsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly PatientsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public PatientsControllerTests()
    {
        _controller = new PatientsController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/patients ???????????????????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var pagedResult = new PagedResult<PatientDto>
        {
            Items = [BuildPatientDto()], TotalCount = 1, Page = 1, PageSize = 20
        };

        _sender.Setup(s => s.Send(It.IsAny<GetPatientsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(pagedResult);
    }

    [Fact]
    public async Task GetAll_PassesQueryParameters_ToSender()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetPatientsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(page: 2, pageSize: 10, search: "John", ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetPatientsQuery>(q => q.Page == 2 && q.PageSize == 10 && q.Search == "John"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/patients/{id} ??????????????????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenPatientExists()
    {
        var patient = BuildPatientDto();
        _sender.Setup(s => s.Send(It.IsAny<GetPatientByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(patient);

        var result = await _controller.GetById(patient.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(patient);
    }

    [Fact]
    public async Task GetById_PassesCorrectId_ToSender()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetPatientByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildPatientDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetPatientByIdQuery>(q => q.Id == id && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/patients ??????????????????????????????????????????????????

    [Fact]
    public async Task Register_ReturnsCreatedAtAction_WithPatientDto()
    {
        var patient = BuildPatientDto();
        var dto = BuildCreateDto();

        _sender.Setup(s => s.Send(It.IsAny<RegisterPatientCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(patient);

        var result = await _controller.Register(dto, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(PatientsController.GetById));
        created.Value.Should().BeSameAs(patient);
    }

    [Fact]
    public async Task Register_SendsCommandWithApplicationId()
    {
        var dto = BuildCreateDto();
        _sender.Setup(s => s.Send(It.IsAny<RegisterPatientCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildPatientDto());

        await _controller.Register(dto, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<RegisterPatientCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/patients/{id} ??????????????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedPatient()
    {
        var id = Guid.NewGuid();
        var patient = BuildPatientDto();
        var dto = BuildUpdateDto();

        _sender.Setup(s => s.Send(It.IsAny<UpdatePatientCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(patient);

        var result = await _controller.Update(id, dto, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(patient);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdatePatientCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildPatientDto());

        await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdatePatientCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/patients/{id} ???????????????????????????????????????????

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<DeletePatientCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        var result = await _controller.Delete(id, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<DeletePatientCommand>(), It.IsAny<CancellationToken>()))
               .Returns(Task.FromResult(Unit.Value));

        await _controller.Delete(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<DeletePatientCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/patients/{id}/upload-photo ???????????????????????????????

    [Fact]
    public async Task UploadPhoto_ReturnsBadRequest_WhenPhotoIsNull()
    {
        var result = await _controller.UploadPhoto(Guid.NewGuid(), null!, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.Value.Should().Be("No photo file provided.");
    }

    [Fact]
    public async Task UploadPhoto_ReturnsBadRequest_WhenPhotoExceeds5Mb()
    {
        var file = BuildFormFile("photo.jpg", "image/jpeg", sizeBytes: 6 * 1024 * 1024);

        var result = await _controller.UploadPhoto(Guid.NewGuid(), file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.Value.Should().Be("Photo file must not exceed 5 MB.");
    }

    [Fact]
    public async Task UploadPhoto_ReturnsBadRequest_WhenExtensionNotAllowed()
    {
        var file = BuildFormFile("photo.gif", "image/gif", sizeBytes: 100);

        var result = await _controller.UploadPhoto(Guid.NewGuid(), file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.Value.Should().Be("Only JPG, PNG, and WEBP images are allowed.");
    }

    [Fact]
    public async Task UploadPhoto_ReturnsOk_WithUrl_WhenValid()
    {
        var id = Guid.NewGuid();
        var file = BuildFormFile("photo.jpg", "image/jpeg", sizeBytes: 100);
        const string expectedUrl = "https://cdn.example.com/photo.jpg";

        _sender.Setup(s => s.Send(It.IsAny<UploadPatientPhotoCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(expectedUrl);

        var result = await _controller.UploadPhoto(id, file, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { url = expectedUrl });
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static PatientDto BuildPatientDto() => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        FirstName = "Jane",
        LastName = "Doe",
        Gender = Gender.Female,
        PhoneNumber = "+1234567890",
        IsActive = true
    };

    private static CreatePatientDto BuildCreateDto() => new(
        "Jane", "Doe", new DateOnly(1995, 4, 20),
        Gender.Female, "+1234567890", null, null, null, null);

    private static UpdatePatientDto BuildUpdateDto() => new(
        "Jane", "Doe", new DateOnly(1995, 4, 20),
        Gender.Female, "+1234567890", null, null, null, null);

    private static PagedResult<PatientDto> EmptyPagedResult() =>
        new PagedResult<PatientDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };

    private static IFormFile BuildFormFile(string fileName, string contentType, long sizeBytes)
    {
        var content = new byte[sizeBytes];
        var stream = new MemoryStream(content);
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        mock.Setup(f => f.OpenReadStream()).Returns(stream);
        return mock.Object;
    }
}
