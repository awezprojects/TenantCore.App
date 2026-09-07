using FluentValidation;
using TenantCore.Application.Features.Rooms.Commands;

namespace TenantCore.Application.Features.Rooms.Validators;

public sealed class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
        RuleFor(x => x.RoomNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.RoomType).MaximumLength(50).When(x => !string.IsNullOrEmpty(x.RoomType));
        RuleFor(x => x.PricePerDay).GreaterThanOrEqualTo(0);
    }
}
