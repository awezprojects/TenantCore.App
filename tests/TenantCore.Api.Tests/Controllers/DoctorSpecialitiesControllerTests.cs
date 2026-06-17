using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Application.Features.DoctorSpecialities.Queries;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class DoctorSpecialitiesControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly DoctorSpecialitiesController _controller;

    public DoctorSpecialitiesControllerTests()
    {
        _controller = new DoctorSpecialitiesController(_sender.Object);
    }

    // ?? GET /api/doctor-specialities ???????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithSpecialityList()
    {
        var specialities = new List<DoctorSpecialityDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Cardiology" },
            new() { Id = Guid.NewGuid(), Name = "Gynaecology" }
        };
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorSpecialitiesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(specialities);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(specialities);
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithEmptyList_WhenNoSpecialities()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorSpecialitiesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<DoctorSpecialityDto>());

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<List<DoctorSpecialityDto>>()
              .Which.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_SendsGetDoctorSpecialitiesQuery()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetDoctorSpecialitiesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<DoctorSpecialityDto>());

        await _controller.GetAll(CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.IsAny<GetDoctorSpecialitiesQuery>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
