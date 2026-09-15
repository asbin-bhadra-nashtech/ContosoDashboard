<!--
Sync Impact Report
- Version change: unversioned scaffold -> 1.0.0
- Modified principles: scaffold placeholders -> five ContosoDashboard governance principles
- Added sections: Training Constraints; Development Workflow
- Removed sections: none
- Follow-up TODOs: confirm the original ratification date
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First Scope
ContosoDashboard changes MUST preserve the repository's purpose as an offline training
application. Features MUST remain understandable, locally runnable, and appropriately
simplified for instruction. Production-only infrastructure or operational complexity MUST
not be introduced without an explicit feature specification and migration rationale.

### II. Layered Design
Application behavior MUST remain separated into Razor UI, services, data access, and
domain models. Business rules MUST live in services or domain-oriented code rather than
being duplicated in components. Infrastructure dependencies MUST use abstractions when
the repository documents a local implementation and a plausible cloud migration path.

### III. Security by Construction
Every protected page and service operation MUST enforce authentication, authorization,
and resource ownership at the appropriate boundary. New data access paths MUST be
reviewed for IDOR, privilege escalation, unsafe input handling, and accidental exposure
of another user's data. Mock authentication MUST remain clearly documented as training-only
and MUST not be represented as production-ready identity management.

### IV. Verifiable Changes
Each feature MUST include acceptance criteria that can be checked through focused tests,
manual verification steps, or both. Changes affecting authorization, persistence, service
contracts, or cross-page workflows MUST include an integration-level check. A change is
not complete until the documented build and relevant validation checks pass.

### V. Simple, Documented Evolution
The smallest coherent implementation MUST be preferred over speculative abstraction.
Public behavior, schema changes, security assumptions, and known training limitations MUST
be documented alongside the change. Breaking changes MUST state their migration impact and
must not silently invalidate existing training exercises.

## Training Constraints

The supported baseline is ASP.NET Core 8.0 with Blazor Server, Entity Framework Core, and
SQL Server LocalDB. The application MUST continue to work offline with local configuration
and seeded training data. Production deployment, real credentials, external identity
providers, cloud storage, and compliance claims are out of scope unless a specification
explicitly changes that boundary. Documentation MUST warn users that the mock security
implementation is not suitable for production.

## Development Workflow

Feature work MUST begin with a clear specification when it changes user-visible behavior,
data contracts, authorization, or architecture. Plans and tasks MUST identify affected
pages, services, models, and validation steps. Reviews MUST check constitution compliance,
security boundaries, offline operation, and documentation impact. The repository build
MUST pass before work is considered complete.

## Governance
<!-- Example: Constitution supersedes all other practices; Amendments require documentation, approval, migration plan -->

This constitution governs repository changes and supersedes conflicting local practice.
Amendments MUST be made through the constitution workflow, include a Sync Impact Report,
and update the version according to the policy below. A MAJOR version is required for a
backward-incompatible removal or redefinition of a principle. A MINOR version is required
for a new principle or materially expanded governance requirement. A PATCH version is
required for clarifications, wording, or non-semantic corrections. The last amended date
MUST be updated for every amendment. Reviews and Spec Kit artifacts MUST verify compliance;
any exception MUST state its reason, scope, and follow-up plan.

**Version**: 1.0.0 | **Ratified**: TODO(RATIFICATION_DATE): confirm original adoption date | **Last Amended**: 2026-09-15
