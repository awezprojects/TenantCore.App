using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Api.Middleware;
using TenantCore.Application.Features.UsgTemplates.Commands;
using TenantCore.Application.Features.UsgTemplates.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class UsgTemplatesControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly UsgTemplatesController _controller;
    private readonly Guid _applicationId = Guid.NewGuid();

    public UsgTemplatesControllerTests()
    {
        _controller = new UsgTemplatesController(_sender.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClinicContextMiddleware.ContextKey] = _applicationId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // ?? GET /api/usg-templates ??????????????????????????????????????????????

    [Fact]
    public async Task GetClinicTemplate_ReturnsOk_WithTemplateDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetClinicUsgTemplateQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetClinicTemplate(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetClinicTemplate_SendsQueryWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetClinicUsgTemplateQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetClinicTemplate(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetClinicUsgTemplateQuery>(q => q.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/usg-templates/default ?????????????????????????????????????

    [Fact]
    public async Task GetDefault_ReturnsOk_WithDefaultTemplate()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetDefaultUsgTemplateQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetDefault(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetDefault_SendsDefaultTemplateQuery()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetDefaultUsgTemplateQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetDefault(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.IsAny<GetDefaultUsgTemplateQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/usg-templates ??????????????????????????????????????????????

    [Fact]
    public async Task Upsert_ReturnsOk_WithUpdatedTemplate()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpsertClinicUsgTemplateCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Upsert(new UpsertClinicUsgTemplateRequest(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Upsert_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<UpsertClinicUsgTemplateCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Upsert(new UpsertClinicUsgTemplateRequest(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpsertClinicUsgTemplateCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? DELETE /api/usg-templates ???????????????????????????????????????????

    [Fact]
    public async Task Reset_ReturnsOk_WithDefaultTemplate()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<ResetClinicUsgTemplateCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Reset(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Reset_SendsCommandWithApplicationId()
    {
        _sender.Setup(s => s.Send(It.IsAny<ResetClinicUsgTemplateCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Reset(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<ResetClinicUsgTemplateCommand>(c => c.ApplicationId == _applicationId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static ClinicUsgTemplateDto BuildDto() => new()
    {
        ApplicationId = Guid.NewGuid(),
        IsCustomized = false,
        Rows = []
    };
}
