# ADR-001: Clean Architecture

**Date**: 2024-01-01
**Status**: Accepted
**Deciders**: TenantCore.App team

## Context

TenantCore.App is a multi-tenant wrapper API that integrates with an external auth service and exposes data to a Blazor WebAssembly frontend. The system needs to be testable, maintainable, and evolvable as tenant and application models grow. The framework (ASP.NET Core) and database (EF Core / SQL Server) should be swappable without touching business logic.

## Decision

Adopt **Clean Architecture** with four explicit layers:

| Layer | Project | Depends On |
|-------|---------|------------|
| Domain | `TenantCore.Domain` | Nothing |
| Application | `TenantCore.Application` | Domain, Shared |
| Infrastructure | `TenantCore.Infrastructure` | Application, Domain |
| API | `TenantCore.Api` | Application, Infrastructure, Shared |

`TenantCore.Shared` holds cross-cutting DTOs and authorization constants that the API surface and Blazor client both reference.

## Rationale

- Domain logic is pure C# — unit-testable without EF Core or ASP.NET Core
- Infrastructure concerns (SQL, HTTP clients) are isolated behind interfaces defined in Domain
- The Application layer owns business workflows via handlers — no framework leakage
- The Blazor client can reference Shared DTOs directly without coupling to the server projects

## Alternatives Considered

| Option | Reason Not Chosen |
|--------|------------------|
| N-Layer (Controller → Service → Repository) | Business logic leaks into services and controllers; harder to test in isolation |
| Vertical Slice Architecture (all code per feature in one folder) | Considered, but Clean Architecture layers provide clearer separation for this team size and domain complexity |
| Minimal API with no layering | Too flat for a multi-entity system with CQRS and authorization requirements |

## Consequences

**Positive**:
- Domain entities and business rules are fully testable without the database
- Each layer can be replaced (e.g., swap SQL Server for PostgreSQL) without touching Application or Domain
- Clear onboarding path: new developers know exactly where each type of code lives

**Negative / Trade-offs**:
- More files per feature (entity, interface, handler, validator, translator, repo, config, DTO, controller)
- Boilerplate increases for simple CRUD — mitigated by the `plan-feature` / `execute-feature` workflow

**Neutral**:
- Translator pattern (static classes) chosen over AutoMapper to keep mappings explicit and refactor-safe

## Compliance

- Migration required: no
- Breaking change: no
- Related: ADR-002 (CQRS with MediatR)
