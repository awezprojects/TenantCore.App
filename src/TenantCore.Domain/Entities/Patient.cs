using TenantCore.Domain.Common;
using TenantCore.Shared.Enums;

namespace TenantCore.Domain.Entities;

public class Patient : AuditableEntity
{
    public Guid ApplicationId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateOnly? DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? AadhaarNumber { get; private set; }
    public string? PhotoUrl { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }

    private Patient() { }

    public static Patient Create(
        Guid applicationId,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        Gender gender,
        string phoneNumber,
        string? email,
        string? aadhaarNumber,
        string? photoUrl,
        string? address) => new()
    {
        Id = Guid.NewGuid(),
        ApplicationId = applicationId,
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = dateOfBirth,
        Gender = gender,
        PhoneNumber = phoneNumber,
        Email = email,
        AadhaarNumber = aadhaarNumber,
        PhotoUrl = photoUrl,
        Address = address,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        Gender gender,
        string phoneNumber,
        string? email,
        string? aadhaarNumber,
        string? photoUrl,
        string? address)
    {
        FirstName = firstName;
        LastName = lastName;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        PhoneNumber = phoneNumber;
        Email = email;
        AadhaarNumber = aadhaarNumber;
        PhotoUrl = photoUrl;
        Address = address;
        SetUpdatedAt();
    }

    public void Deactivate()
    {
        IsActive = false;
        SetUpdatedAt();
    }
}
