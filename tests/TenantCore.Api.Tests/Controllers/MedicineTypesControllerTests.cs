using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Application.Features.MedicineTypes.Commands;
using TenantCore.Application.Features.MedicineTypes.Queries;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class MedicineTypesControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly MedicineTypesController _controller;

    public MedicineTypesControllerTests()
    {
        _controller = new MedicineTypesController(_sender.Object);
    }

    // ?? GET /api/medicine-types ?????????????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var pagedResult = new PagedResult<MedicineTypeDto>
        {
            Items = [BuildDto()], TotalCount = 1, Page = 1, PageSize = 20
        };
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineTypesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(pagedResult);
    }

    [Fact]
    public async Task GetAll_PassesQueryParameters_ToSender()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineTypesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(page: 2, pageSize: 5, search: "tablet", ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetMedicineTypesQuery>(q => q.Page == 2 && q.PageSize == 5 && q.Search == "tablet"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/medicine-types/{id} ????????????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineTypeByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.GetById(dto.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task GetById_SendsQueryWithCorrectId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineTypeByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetMedicineTypeByIdQuery>(q => q.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/medicine-types ????????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithDto()
    {
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateMedicineTypeCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Create(new CreateMedicineTypeDto("Antibiotic", null), CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(MedicineTypesController.GetById));
        created.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Create_SendsCommandWithNameAndDescription()
    {
        _sender.Setup(s => s.Send(It.IsAny<CreateMedicineTypeCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Create(new CreateMedicineTypeDto("Antibiotic", "desc"), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateMedicineTypeCommand>(c => c.Name == "Antibiotic" && c.Description == "desc"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/medicine-types/{id} ????????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedDto()
    {
        var id = Guid.NewGuid();
        var dto = BuildDto();
        _sender.Setup(s => s.Send(It.IsAny<UpdateMedicineTypeCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(dto);

        var result = await _controller.Update(id, new UpdateMedicineTypeDto("Antibiotic", null, true), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(dto);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdateMedicineTypeCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildDto());

        await _controller.Update(id, new UpdateMedicineTypeDto("X", null, true), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateMedicineTypeCommand>(c => c.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static MedicineTypeDto BuildDto() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Antibiotic",
        IsActive = true
    };

    private static PagedResult<MedicineTypeDto> EmptyPagedResult() =>
        new PagedResult<MedicineTypeDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };
}
