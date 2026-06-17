using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.Prescriptions.Commands;
using TenantCore.Application.Features.Prescriptions.Queries;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Api.Tests.Controllers;

public class PrescriptionsControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly PrescriptionsController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public PrescriptionsControllerTests()
    {
        _controller = new PrescriptionsController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/prescriptions ??????????????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var paged = new PagedResult<PrescriptionDto>
        {
            Items = [BuildDto()], TotalCount = 1, Page = 1, PageSize = 20
        };
        _sender.Setup(s => s.Send(It.IsAny<GetPrescriptionsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(paged);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(paged);
    }

    [Fact]
    public async Task GetAll_PassesApplicationIdFromContext()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetPrescriptionsQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetPrescriptionsQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/prescriptions/{id} ?????????????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetPrescriptionByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetById_SendsQueryWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetPrescriptionByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetPrescriptionByIdQuery>(q => q.Id == id && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/prescriptions/opd/{opdRegistrationId} ?????????????????????

    [Fact]
    public async Task GetByOpdId_ReturnsOk_WhenFound()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetPrescriptionByOpdIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetByOpdId(Guid.NewGuid(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetByOpdId_SendsQueryWithCorrectIds()
    {
        var opdId = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetPrescriptionByOpdIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetByOpdId(opdId, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetPrescriptionByOpdIdQuery>(q => q.OpdRegistrationId == opdId && q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/prescriptions ?????????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithPrescriptionDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreatePrescriptionCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(BuildCreateDto(), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(PrescriptionsController.GetById));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreatePrescriptionCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(BuildCreateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreatePrescriptionCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/prescriptions/{id} ?????????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpdatePrescriptionCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdatePrescriptionCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdatePrescriptionCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/prescriptions/{id}/submit ????????????????????????????????

    [Fact]
    public async Task Submit_ReturnsOk_WithSubmittedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<SubmitPrescriptionCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Submit(id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Submit_SendsCommandWithCorrectIdAndApplicationId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<SubmitPrescriptionCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Submit(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<SubmitPrescriptionCommand>(c => c.Id == id && c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/prescriptions/{id}/reports ???????????????????????????????

    [Fact]
    public async Task UploadReport_ReturnsBadRequest_WhenFileIsNull()
    {
        var result = await _controller.UploadReport(Guid.NewGuid(), null!, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.Value.Should().Be("No file provided.");
    }

    [Fact]
    public async Task UploadReport_ReturnsBadRequest_WhenFileExceeds50Mb()
    {
        var file = BuildFormFile("report.pdf", "application/pdf", 51 * 1024 * 1024);

        var result = await _controller.UploadReport(Guid.NewGuid(), file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.Value.Should().Be("File exceeds the 50 MB limit.");
    }

    [Fact]
    public async Task UploadReport_ReturnsBadRequest_WhenExtensionNotAllowed()
    {
        var file = BuildFormFile("report.txt", "text/plain", 100);

        var result = await _controller.UploadReport(Guid.NewGuid(), file, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.Value.Should().Be("File type not allowed. Accepted: PDF, JPG, PNG, BMP.");
    }

    [Fact]
    public async Task UploadReport_ReturnsCreatedAtAction_WhenValid()
    {
        var id = Guid.NewGuid();
        var file = BuildFormFile("report.pdf", "application/pdf", 100);
        var reportDto = new PrescriptionReportDto { Id = Guid.NewGuid(), PrescriptionId = id };

        _sender.Setup(s => s.Send(It.IsAny<UploadPrescriptionReportCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(reportDto);

        var result = await _controller.UploadReport(id, file, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(PrescriptionsController.GetById));
        created.Value.Should().BeSameAs(reportDto);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static PrescriptionDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = Guid.NewGuid(),
        OpdRegistrationId = Guid.NewGuid(),
        PatientId = Guid.NewGuid(),
        PatientName = "Jane Doe",
        DoctorUserId = Guid.NewGuid(),
        DoctorName = "Dr. Smith",
        PrescriptionNumber = "RX-001",
        Status = PrescriptionStatus.Draft
    };

    private static CreatePrescriptionDto BuildCreateDto() => new(
        Guid.NewGuid(), Guid.NewGuid(), "Dr. Smith",
        null, null, [], null, null, null, null, null, null, null, null, []);

    private static UpdatePrescriptionDto BuildUpdateDto() => new(
        null, null, [], null, null, null, null, null, null, null, null, []);

    private static PagedResult<PrescriptionDto> EmptyPagedResult() =>
        new PagedResult<PrescriptionDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };

    private static IFormFile BuildFormFile(string fileName, string contentType, long sizeBytes)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.ContentType).Returns(contentType);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock.Object;
    }
}
