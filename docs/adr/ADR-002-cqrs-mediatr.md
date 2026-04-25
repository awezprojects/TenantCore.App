# ADR-002: CQRS with MediatR

**Date**: 2024-01-01
**Status**: Accepted
**Deciders**: TenantCore.App team

## Context

The Application layer needs a consistent way to dispatch commands (writes) and queries (reads) from the API layer to business logic, while enforcing cross-cutting concerns like validation and logging without scattering them across every handler.

## Decision

Use **MediatR** to implement CQRS in the Application layer:

- Commands and queries are `sealed record` types implementing `IRequest<T>`
- Handlers are `sealed class` types implementing `IRequestHandler<TRequest, TResponse>`
- Cross-cutting concerns (validation, logging) are implemented as `IPipelineBehavior<T>` and run automatically for every request
- Controllers are thin: they only call `sender.Send(command)` and return the result

## Rationale

- Commands and queries are explicit, named objects — readable, refactorable, and independently testable
- `ValidationBehavior` runs FluentValidation before every handler — no manual `ModelState.IsValid` checks in controllers
- `LoggingBehavior` traces every request without touching handler code
- The controller never imports business services directly, preventing logic creep into the API layer
- Assembly scanning auto-registers all handlers and validators — no manual DI wiring per feature

## Alternatives Considered

| Option | Reason Not Chosen |
|--------|------------------|
| Direct service injection in controllers | Handlers would accumulate multiple service dependencies; harder to unit-test controllers in isolation |
| Custom mediator (no library) | Additional maintenance burden; MediatR is well-established in .NET Clean Architecture projects |
| No CQRS (single service per entity) | Read and write paths share the same service — harder to optimize queries independently; violates SRP at scale |

## Consequences

**Positive**:
- Validation is enforced globally via the pipeline — impossible to bypass by forgetting a check
- Each handler is a small, focused unit — easy to test with a mock repository
- New features follow a predictable file structure (see `plan-feature` workflow)

**Negative / Trade-offs**:
- MediatR adds indirection — debugging requires knowing the pipeline order
- `sealed record` commands are verbose for complex inputs compared to builder patterns
- Assembly scanning requires handlers and validators to be in the correct project

**Neutral**:
- FluentValidation chosen over DataAnnotations — richer rules, unit-testable validators
- Translators are static classes, not AutoMapper profiles — explicit and refactor-safe

## Compliance

- Migration required: no
- Breaking change: no
- Related: ADR-001 (Clean Architecture)
