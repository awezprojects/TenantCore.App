# ADR-009 — Unit Testing Standards

**Repo:** TenantCore.App  
**Status:** Active  
**Layer:** `tests/TenantCore.Application.Tests`, `tests/TenantCore.Domain.Tests`

---

## Context

Currently, `TenantCore.Application.Tests` and `TenantCore.Domain.Tests` exist but coverage is incomplete. This ADR defines the **target standard** for testing in TenantCore.App — both what exists now and what must be followed when adding new tests.

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
│   │   │   ├── CreatePatientCommandHandlerTests.cs
│   │   │   └── UpdatePatientCommandHandlerTests.cs
│   │   ├── Queries/
│   │   │   └── GetPatientByIdQueryHandlerTests.cs
│   │   ├── Validators/
│   │   │   └── CreatePatientCommandValidatorTests.cs
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
    public async Task Handle_NonExistentPatient_ReturnsNull()
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
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
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
        // Arrange
        var command = new CreatePatientCommand
        {
            FirstName = "John",
            LastName = "Doe",
            ApplicationId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyFirstName_FailsWithMessage(string firstName)
    {
        // Arrange
        var command = new CreatePatientCommand
        {
            FirstName = firstName,
            LastName = "Doe",
            ApplicationId = Guid.NewGuid()
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePatientCommand.FirstName));
    }
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

## What TO Test

| Layer | Test | Priority |
|-------|------|---------|
| Command handlers | That `AddAsync` and `SaveChangesAsync` are called with correct data | High |
| Command handlers | That `ApplicationId` is set correctly on the entity | High |
| Query handlers | That repository is called with correct parameters | High |
| Query handlers | That entities are correctly mapped to DTOs | High |
| Validators | That required fields fail when empty | High |
| Validators | That max-length rules are enforced | Medium |
| Validators | That valid commands pass without errors | High |
| Translators | That all fields map correctly in both directions | Medium |
| Pipeline behaviors | That `ValidationBehavior` throws on validation failure | Medium |

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
