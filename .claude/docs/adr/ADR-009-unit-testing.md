# ADR-009 — Unit Testing Standards

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** `tests/TenantCore.Application.Tests`, `tests/TenantCore.Domain.Tests`

---

## Context

Currently, `TenantCore.Application.Tests` and `TenantCore.Domain.Tests` exist but coverage is incomplete. This ADR defines the **target standard** for testing in TenantCore.App — both what exists now and what must be followed when adding new tests.

**Every new feature must ship with tests.** No feature is considered implemented until the test files described in the "Mandatory Coverage Per Feature" section below are written and passing.

---

## Test Projects and Their Scope

| Project | Tests | What It Covers |
|---------|-------|---------------|
| `TenantCore.Application.Tests` | Unit tests | MediatR handlers, validators, translators |
| `TenantCore.Domain.Tests` | Unit tests | Domain entity logic, base class behavior |

**Not currently tested (future scope):**
- `TenantCore.Infrastructure` — Integration tests with SQL Server or EF InMemory
- `TenantCore.Api` — End-to-end API tests via `WebApplicationFactory`

---

## Technology Stack

| Tool | Package | Version | Purpose |
|------|---------|---------|---------|
| Test runner | `xunit` | 2.4.2 | Test framework |
| Mocking | `Moq` | latest | Mock repository and service interfaces |
| Assertions | `FluentAssertions` | latest | Readable assertion syntax |
| Test SDK | `Microsoft.NET.Test.Sdk` | latest | MSBuild integration |

---

## Test Organization

Tests mirror the production project structure:

```
TenantCore.Application.Tests/
├── Features/
│   ├── Patients/
│   │   ├── Commands/
│   │   │   ├── CreatePatientHandlerTests.cs
│   │   │   ├── UpdatePatientHandlerTests.cs
│   │   │   └── DeletePatientHandlerTests.cs
│   │   ├── Queries/
│   │   │   ├── GetPatientsHandlerTests.cs
│   │   │   └── GetPatientByIdHandlerTests.cs
│   │   ├── Validators/
│   │   │   ├── CreatePatientCommandValidatorTests.cs
│   │   │   └── UpdatePatientCommandValidatorTests.cs
│   │   └── Translators/
│   │       └── PatientTranslatorTests.cs
│   ├── Medicines/
│   │   └── ...
│   └── Prescriptions/
│       └── ...
└── Common/
    └── Behaviors/
        ├── LoggingBehaviorTests.cs
        └── ValidationBehaviorTests.cs

TenantCore.Domain.Tests/
└── Common/
    ├── BaseEntityTests.cs
    └── AuditableEntityTests.cs
```

---

## Test Naming Convention

Use the **Given_When_Then** (or **MethodName_Scenario_ExpectedResult**) convention:

```csharp
// Format: {MethodOrAction}_{Scenario}_{ExpectedResult}
[Fact]
public async Task Handle_ValidCommand_ReturnsNewPatientId()

[Fact]
public async Task Handle_PatientNotFound_ThrowsEntityNotFoundException()

[Fact]
public void Validate_EmptyFirstName_ReturnsValidationError()

[Fact]
public void ToEntity_ValidCommand_MapsAllFieldsCorrectly()
```

---

## Handler Test Pattern

### Command Handler Test

```csharp
public class CreatePatientCommandHandlerTests
{
    private readonly Mock<IPatientRepository> _repositoryMock;
    private readonly CreatePatientCommandHandler _handler;

    public CreatePatientCommandHandlerTests()
    {
        _repositoryMock = new Mock<IPatientRepository>();
        _handler = new CreatePatientCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsPatientAndReturnsId()
    {
        // Arrange
        var command = new CreatePatientCommand
        {
            FirstName = "John",
            LastName = "Doe",
            ApplicationId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Patient>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsApplicationIdOnEntity()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new CreatePatientCommand
        {
            FirstName = "Jane",
            ApplicationId = applicationId
        };

        Patient capturedPatient = null!;
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Patient>()))
            .Callback<Patient>(p => capturedPatient = p);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedPatient.ApplicationId.Should().Be(applicationId);
    }
}
```

### Query Handler Test

```csharp
public class GetPatientByIdQueryHandlerTests
{
    private readonly Mock<IPatientRepository> _repositoryMock;
    private readonly GetPatientByIdQueryHandler _handler;

    public GetPatientByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<IPatientRepository>();
        _handler = new GetPatientByIdQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPatient_ReturnsMappedDto()
    {
        // Arrange
        var patientId = Guid.NewGuid();
        var patient = new Patient { Id = patientId, FirstName = "John", LastName = "Doe" };

        _repositoryMock
            .Setup(r => r.GetByIdAsync(patientId))
            .ReturnsAsync(patient);

        var query = new GetPatientByIdQuery
        {
            PatientId = patientId,
            ApplicationId = Guid.NewGuid()
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(patientId);
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task Handle_NonExistentPatient_ThrowsEntityNotFoundException()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Patient?)null);

        var query = new GetPatientByIdQuery
        {
            PatientId = Guid.NewGuid(),
            ApplicationId = Guid.NewGuid()
        };

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<EntityNotFoundException>();
    }
}
```

---

## Validator Test Pattern

```csharp
public class CreatePatientCommandValidatorTests
{
    private readonly CreatePatientCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var command = new CreatePatientCommand(
            new CreatePatientRequest { FirstName = "John", LastName = "Doe" },
            ApplicationId: Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_FirstNameNullOrEmpty_FailsWithError(string? firstName)
    {
        var command = new CreatePatientCommand(
            new CreatePatientRequest { FirstName = firstName!, LastName = "Doe" },
            ApplicationId: Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("FirstName"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_LastNameNullOrEmpty_FailsWithError(string? lastName)
    {
        var command = new CreatePatientCommand(
            new CreatePatientRequest { FirstName = "John", LastName = lastName! },
            ApplicationId: Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("LastName"));
    }

    [Fact]
    public void Validate_FirstNameAtMaxLength_PassesValidation()
    {
        var command = new CreatePatientCommand(
            new CreatePatientRequest { FirstName = new string('A', 100), LastName = "Doe" },
            ApplicationId: Guid.NewGuid());

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_FirstNameExceedsMaxLength_FailsWithError()
    {
        var command = new CreatePatientCommand(
            new CreatePatientRequest { FirstName = new string('A', 101), LastName = "Doe" },
            ApplicationId: Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("FirstName"));
    }

    [Fact]
    public void Validate_EmptyApplicationId_FailsWithError()
    {
        var command = new CreatePatientCommand(
            new CreatePatientRequest { FirstName = "John", LastName = "Doe" },
            ApplicationId: Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ApplicationId"));
    }
}
```

---

## Tenant Isolation Test Pattern

Write one test per Update/Delete handler and GetById handler to verify cross-tenant isolation. The entity exists in the database but belongs to a different `ApplicationId` — the handler must treat this as "not found".

```csharp
[Fact]
public async Task Handle_EntityBelongingToDifferentTenant_ThrowsEntityNotFoundException()
{
    // Arrange
    var commandApplicationId = Guid.NewGuid();
    var entityApplicationId = Guid.NewGuid(); // different tenant

    // GetByIdAsync returns an entity, but it belongs to a different tenant
    // In production the repository filters by applicationId, so this won't be returned.
    // In tests, simulate by returning null (the correct repository behavior).
    _repositoryMock
        .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), commandApplicationId))
        .ReturnsAsync((Patient?)null);

    var command = new UpdatePatientCommand(Guid.NewGuid(), new UpdatePatientRequest(), commandApplicationId);

    // Act
    var act = async () => await _handler.Handle(command, CancellationToken.None);

    // Assert
    await act.Should().ThrowAsync<EntityNotFoundException>();
}
```

---

## Translator Test Pattern

```csharp
public class PatientTranslatorTests
{
    [Fact]
    public void ToEntity_ValidCommand_MapsAllProperties()
    {
        // Arrange
        var command = new CreatePatientCommand
        {
            FirstName = "John",
            LastName = "Doe",
            ApplicationId = Guid.NewGuid()
        };

        // Act
        var entity = PatientTranslator.ToEntity(command);

        // Assert
        entity.FirstName.Should().Be(command.FirstName);
        entity.LastName.Should().Be(command.LastName);
        entity.ApplicationId.Should().Be(command.ApplicationId);
        entity.Id.Should().NotBeEmpty();  // Guid was generated
    }

    [Fact]
    public void ToDto_ValidEntity_MapsAllProperties()
    {
        // Arrange
        var entity = new Patient
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var dto = PatientTranslator.ToDto(entity);

        // Assert
        dto.Id.Should().Be(entity.Id);
        dto.FirstName.Should().Be(entity.FirstName);
    }
}
```

---

## Mandatory Coverage Per Feature

Every new feature **must** include all of the following test scenarios before it is considered complete.

### Create Handler — required tests

| Test name | Scenario |
|-----------|---------|
| `Handle_ValidCommand_ReturnsNewId` | Happy path — entity created, `AddAsync` and `SaveChangesAsync` called exactly once, non-empty Guid returned |
| `Handle_ValidCommand_SetsApplicationIdOnEntity` | Captures entity via `Callback`, asserts `ApplicationId` matches the command value |
| `Handle_ValidCommand_MapsAllFieldsToEntity` | Captures entity, asserts every field maps from request (no field silently dropped) |

### Update Handler — required tests

| Test name | Scenario |
|-----------|---------|
| `Handle_ValidCommand_UpdatesEntityFields` | Entity found, all mutable fields updated, `SaveChangesAsync` called once |
| `Handle_EntityNotFound_ThrowsEntityNotFoundException` | Repository returns `null`, handler throws `EntityNotFoundException` |
| `Handle_EntityBelongingToDifferentTenant_ThrowsEntityNotFoundException` | Entity exists but `ApplicationId` does not match — handler must not update it |

### Delete Handler — required tests

| Test name | Scenario |
|-----------|---------|
| `Handle_ValidCommand_DeletesEntityAndSavesChanges` | Entity found, `Delete` called, `SaveChangesAsync` called once |
| `Handle_EntityNotFound_ThrowsEntityNotFoundException` | Repository returns `null`, handler throws `EntityNotFoundException` |

### GetById Handler — required tests

| Test name | Scenario |
|-----------|---------|
| `Handle_ExistingEntity_ReturnsMappedDto` | Entity found, all DTO fields populated correctly |
| `Handle_NonExistentEntity_ThrowsEntityNotFoundException` | Repository returns `null`, handler throws `EntityNotFoundException` |

### GetAll Handler — required tests

| Test name | Scenario |
|-----------|---------|
| `Handle_EntitiesExist_ReturnsMappedSummaryDtos` | Returns list of DTOs in correct shape |
| `Handle_NoEntities_ReturnsEmptyList` | Repository returns empty enumerable — handler returns empty, does not throw |

### Validator — required tests for each validator

| Test name | Scenario |
|-----------|---------|
| `Validate_ValidCommand_PassesValidation` | All fields present and within limits — `IsValid` is `true`, no errors |
| `Validate_<FieldName>Empty_FailsWithError` | One test per required field — `null` and empty string both fail (`[Theory]` with `[InlineData]`) |
| `Validate_<FieldName>ExceedsMaxLength_FailsWithError` | String one character over `MaxLength` limit — fails |
| `Validate_<FieldName>AtMaxLength_PassesValidation` | String exactly at `MaxLength` limit — passes |
| `Validate_ApplicationIdEmpty_FailsWithError` | `ApplicationId == Guid.Empty` fails |

### Translator — required tests

| Test name | Scenario |
|-----------|---------|
| `ToEntity_ValidCommand_MapsAllProperties` | Every property on the entity is asserted (no field missed) — `Id` is non-empty Guid |
| `ToEntity_SetsApplicationId` | `ApplicationId` on the entity matches the argument passed to `ToEntity` |
| `ToDto_ValidEntity_MapsAllProperties` | Every property on the DTO is asserted |
| `ToSummaryDto_ValidEntity_MapsDisplayFields` | Summary DTO contains only the fields it is supposed to expose |

---

## What TO Test

| Layer | Test | Priority |
|-------|------|---------|
| Command handlers | `AddAsync` and `SaveChangesAsync` called with correct data | High |
| Command handlers | `ApplicationId` is set correctly on the entity | High |
| Command handlers | Every field from the request is mapped to the entity | High |
| Update/Delete handlers | `EntityNotFoundException` thrown when entity is not found | High |
| Update/Delete handlers | Entity from a different tenant is treated as not found | High |
| Query handlers | `EntityNotFoundException` thrown when entity is not found by ID | High |
| Query handlers | Entities are correctly mapped to DTOs — all fields | High |
| Query handlers | Empty result set returns empty list, not an exception | High |
| Validators | Each required field fails when `null` or empty string (use `[Theory]`) | High |
| Validators | Fields exceeding `MaxLength` fail | High |
| Validators | Fields at exactly `MaxLength` pass | Medium |
| Validators | `ApplicationId == Guid.Empty` fails | High |
| Validators | Fully valid command passes with `IsValid == true` | High |
| Translators | All fields are mapped in both directions | Medium |
| Translators | `Id` is a newly generated, non-empty Guid | Medium |
| Pipeline behaviors | `ValidationBehavior` throws on validation failure | Medium |

---

## What NOT to Test

- Do NOT test EF Core behavior (that's the framework's job)
- Do NOT test `SaveChangesAsync` internals — mock the repository and verify the call was made
- Do NOT write tests that only test `null == null` — tests must verify real behavior
- Do NOT test DTOs or entity property setters (no logic there to test)
- Do NOT test `Program.cs` wiring or DI registration

---

## Moq Patterns to Follow

```csharp
// Setup a return value
mock.Setup(r => r.GetByIdAsync(patientId)).ReturnsAsync(patient);

// Verify a method was called once with any argument
mock.Verify(r => r.AddAsync(It.IsAny<Patient>()), Times.Once);

// Verify a method was called with a specific argument
mock.Verify(r => r.SaveChangesAsync(), Times.Once);

// Capture the argument passed to a method
Patient? captured = null;
mock.Setup(r => r.AddAsync(It.IsAny<Patient>()))
    .Callback<Patient>(p => captured = p);
```

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run specific project
dotnet test tests/TenantCore.Application.Tests/

# Run with coverage (optional)
dotnet test --collect:"XPlat Code Coverage"
```
