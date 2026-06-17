using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TenantCore.Api.Controllers;
using TenantCore.Application.Features.Medicines.Commands;
using TenantCore.Application.Features.Medicines.Queries;
using TenantCore.Shared.Common;
using TenantCore.Shared.Dtos;

namespace TenantCore.Api.Tests.Controllers;

public class MedicinesControllerTests
{
    private readonly Mock<ISender> _sender = new();
    private readonly MedicinesController _controller;

    public MedicinesControllerTests()
    {
        _controller = new MedicinesController(_sender.Object);
    }

    // ?? GET /api/medicines ??????????????????????????????????????????????????

    [Fact]
    public async Task GetAll_ReturnsOk_WithPagedResult()
    {
        var pagedResult = new PagedResult<MedicineDto>
        {
            Items = [BuildMedicineDto()], TotalCount = 1, Page = 1, PageSize = 20
        };
        _sender.Setup(s => s.Send(It.IsAny<GetMedicinesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(pagedResult);

        var result = await _controller.GetAll(ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(pagedResult);
    }

    [Fact]
    public async Task GetAll_PassesQueryParameters_ToSender()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetMedicinesQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(EmptyPagedResult());

        await _controller.GetAll(page: 2, pageSize: 5, search: "para", ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetMedicinesQuery>(q => q.Page == 2 && q.PageSize == 5 && q.Search == "para"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/medicines/{id} ?????????????????????????????????????????????

    [Fact]
    public async Task GetById_ReturnsOk_WhenMedicineFound()
    {
        var medicine = BuildMedicineDto();
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(medicine);

        var result = await _controller.GetById(medicine.Id, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(medicine);
    }

    [Fact]
    public async Task GetById_SendsQueryWithCorrectId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineByIdQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildMedicineDto());

        await _controller.GetById(id, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetMedicineByIdQuery>(q => q.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? POST /api/medicines ?????????????????????????????????????????????????

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithMedicineDto()
    {
        var medicine = BuildMedicineDto();
        var dto = BuildCreateDto();

        _sender.Setup(s => s.Send(It.IsAny<CreateMedicineCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(medicine);

        var result = await _controller.Create(dto, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(MedicinesController.GetById));
        created.Value.Should().BeSameAs(medicine);
    }

    [Fact]
    public async Task Create_SendsCommandWithMappedFields()
    {
        var dto = BuildCreateDto();
        _sender.Setup(s => s.Send(It.IsAny<CreateMedicineCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildMedicineDto());

        await _controller.Create(dto, CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<CreateMedicineCommand>(c => c.Name == dto.Name && c.IsGeneric == dto.IsGeneric),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? GET /api/medicines/autocomplete ????????????????????????????????????

    [Fact]
    public async Task Autocomplete_ReturnsOk_WithMedicineList()
    {
        var medicines = new List<MedicineDto> { BuildMedicineDto() };
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineAutocompleteQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(medicines);

        var result = await _controller.Autocomplete("para", ct: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(medicines);
    }

    [Fact]
    public async Task Autocomplete_SendsQueryWithNameAndLimit()
    {
        _sender.Setup(s => s.Send(It.IsAny<GetMedicineAutocompleteQuery>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new List<MedicineDto>());

        await _controller.Autocomplete("asp", limit: 3, ct: CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<GetMedicineAutocompleteQuery>(q => q.Name == "asp" && q.Limit == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? PUT /api/medicines/{id} ?????????????????????????????????????????????

    [Fact]
    public async Task Update_ReturnsOk_WithUpdatedMedicine()
    {
        var id = Guid.NewGuid();
        var medicine = BuildMedicineDto();
        var dto = BuildUpdateDto();

        _sender.Setup(s => s.Send(It.IsAny<UpdateMedicineCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(medicine);

        var result = await _controller.Update(id, dto, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(medicine);
    }

    [Fact]
    public async Task Update_SendsCommandWithCorrectId()
    {
        var id = Guid.NewGuid();
        _sender.Setup(s => s.Send(It.IsAny<UpdateMedicineCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(BuildMedicineDto());

        await _controller.Update(id, BuildUpdateDto(), CancellationToken.None);

        _sender.Verify(s => s.Send(
            It.Is<UpdateMedicineCommand>(c => c.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ?? Helpers ?????????????????????????????????????????????????????????????

    private static MedicineDto BuildMedicineDto() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Paracetamol",
        IsActive = true
    };

    private static CreateMedicineDto BuildCreateDto() => new(
        "Paracetamol", null, null, null, null, null, null, null, null,
        false, null, null, null, null, null, null);

    private static UpdateMedicineDto BuildUpdateDto() => new(
        "Paracetamol", null, null, null, null, null, null, null, null,
        false, null, null, null, null, null, true, null);

    private static PagedResult<MedicineDto> EmptyPagedResult() =>
        new PagedResult<MedicineDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };
}
