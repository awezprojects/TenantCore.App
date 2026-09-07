using MediatR;
using TenantCore.Shared.Dtos;
using TenantCore.Shared.Enums;

namespace TenantCore.Application.Features.DoctorProfiles.Commands;

public sealed record SetDoctorPrescriptionTemplateCommand(Guid UserId, PrescriptionTemplate Template) : IRequest<DoctorProfileDto>;
