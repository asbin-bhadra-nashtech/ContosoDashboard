# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command; its definition describes the execution workflow.

## Summary

Add secure, offline-capable document upload and management to the existing ContosoDashboard. Extend the EF Core model and service layers, store validated binaries outside `wwwroot` through `IFileStorageService`, apply ownership/project/share authorization in `IDocumentService`, and expose authorized content through a protected HTTP stream contract. Reuse existing notification, dashboard, project, task, and authentication services.

## Technical Context

**Language/Version**: C# / .NET 8.0, nullable reference types enabled

**Primary Dependencies**: ASP.NET Core Blazor Server, Entity Framework Core 8 SQL Server provider, existing cookie authentication and Bootstrap UI

**Storage**: SQL Server LocalDB for metadata plus local filesystem under configured `AppData/uploads`; storage and scanning abstractions preserve future Azure migration

**Testing**: `dotnet build .\ContosoDashboard.sln` plus focused xUnit service/storage tests, protected content endpoint integration tests, and Blazor/manual integration checks in `quickstart.md`

**Target Platform**: Offline Windows training environment with ASP.NET Core server, browser client, and SQL Server LocalDB

**Project Type**: Single-project server-rendered web application with Blazor Server UI and protected HTTP content endpoint

**Performance Goals**: Upload valid files up to 25 MB within 30 seconds, list up to 500 documents within 2 seconds, search within 2 seconds, and previews within 3 seconds under representative conditions

**Constraints**: Offline-first; local filesystem for training; files outside `wwwroot`; integer document keys; text categories; existing mock claims and role hierarchy; fail closed when scanning is unavailable; no cloud dependency; one document per submission; task-derived project association; department-based team sharing; retained delete identity in audit details

**Scale/Scope**: Existing seeded users and projects, up to 500 accessible documents per list/search scenario, five priority journeys, and one feature area spanning models, services, pages, dashboard, projects, tasks, notifications, and audit reporting

## Clarified Design Decisions

- Team shares use `User.Department`; access follows the current department and active share state, while notifications go only to eligible users with in-app notifications enabled.
- Each upload submission contains exactly one document with independent metadata, scanning, progress, transaction, and result state.
- A selected task is authoritative for project association. Tasks without a project and conflicting client project values are rejected.
- Delete activity retains sanitized document ID/title details while the nullable `DocumentId` relationship is set to null.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The design passes the constitution gates:

- **Training-first**: remains local, offline, and understandable; production scanner/cloud migration are explicit boundaries.
- **Layered design**: UI, service authorization, EF persistence, storage, and scanning remain separated.
- **Security by construction**: service authorization precedes metadata/content access; files are outside `wwwroot`; claims and role isolation are reused.
- **Verifiable changes**: independent acceptance scenarios, quickstart checks, and focused automated coverage are defined.
- **Simple documented evolution**: one existing project is extended with only necessary entities, services, and abstractions.

**Gate status**: PASS. No constitution violations require a complexity exception.

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/ApplicationDbContext.cs              # Document, share, activity sets and relationships
├── Models/                                   # Document, DocumentShare, DocumentActivity entities
├── Services/                                 # IDocumentService, storage, scanner, authorization, audit
├── Pages/                                    # Documents, project/task integration, admin audit UI
├── Program.cs                                # Dependency injection and protected content endpoint
├── Migrations/                                # Document schema migration and model snapshot
└── AppData/uploads/                           # Runtime local storage, outside wwwroot

ContosoDashboard.Tests/                       # New focused automated test project if feasible
├── Services/                                 # Validation, authorization, cleanup, reporting tests
└── Integration/                              # EF/storage/content endpoint checks
```

**Structure Decision**: Extend the existing `ContosoDashboard` web project in its established directories, keep document binaries behind `IFileStorageService`, and add the existing focused test project for service and endpoint coverage. Do not introduce a separate backend, frontend, team-management entity, or cloud dependency. The feature's design artifacts remain under `specs/001-document-upload-management`.

## Complexity Tracking

No constitution violations. Complexity tracking is not required.
