---

description: "Task list for Document Upload and Management"
---

# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`

**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/document-content-api.md](contracts/document-content-api.md)

**Tests**: The feature specification defines independently testable acceptance scenarios but does not explicitly require TDD or automated test-first development. Focused validation tasks are included in the final phase and should be promoted to automated tests if the implementation adds a test project.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the existing ContosoDashboard project for document storage and focused validation.

- [X] T001 Add the configured local upload-root and document limits to `ContosoDashboard/appsettings.json` and `ContosoDashboard/appsettings.Development.json`, keeping the runtime root outside `wwwroot`
- [X] T002 [P] Add the focused test project `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` targeting .NET 8 and reference `ContosoDashboard/ContosoDashboard.csproj`
- [X] T003 [P] Add `ContosoDashboard.Tests/ContosoDashboard.Tests.csproj` to `ContosoDashboard.sln` at the repository root so the solution build includes it
- [X] T004 [P] Document local upload storage, scanner limitations, and reset instructions in `README.md`

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared entities, persistence, infrastructure abstractions, and authorization primitives before story work.

- [X] T005 Add `Document`, `DocumentShare`, and `DocumentActivity` entities with integer keys, required/optional fields, approved text categories, 255-character MIME support, and 25 MB size validation in `ContosoDashboard/Models/Document.cs`
- [X] T006 Add document navigation properties to `ContosoDashboard/Models/User.cs`, `ContosoDashboard/Models/Project.cs`, and `ContosoDashboard/Models/TaskItem.cs` without weakening existing relationships
- [X] T007 Register document DbSets, foreign keys, delete behaviors, uniqueness constraints for active shares, and indexes for owner, project, upload date, category, and share lookups in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T008 [P] Define `IFileStorageService`, `IFileScanner`, storage options, and upload result/error contracts in `ContosoDashboard/Services/FileStorageService.cs`
- [X] T009 [P] Implement `LocalFileStorageService` with GUID-based relative keys, path traversal protection, directory creation, stream copy, download, and delete behavior in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T010 [P] Implement the offline scanner policy that fails closed when scanning is unavailable and supports deterministic training validation in `ContosoDashboard/Services/LocalFileScanner.cs`
- [X] T011 Register the storage, scanner, and document services in `ContosoDashboard/Program.cs` and ensure `AppData/uploads` is never mapped by `UseStaticFiles`
- [X] T012 Add shared document constants for approved categories, MIME/extension allowlists, 25 MB limits, and safe filename handling in `ContosoDashboard/Services/DocumentValidation.cs`

**Checkpoint**: Foundation ready. EF metadata, local storage, scanner policy, and dependency injection are available to all user stories.

## Phase 3: User Story 1 - Upload and Organize a Document (Priority: P1) 🎯 MVP

**Goal**: Authenticated employees can upload supported files with required metadata and optional project/task context, with secure storage and clear progress/errors.

**Independent Test**: Sign in as an employee, upload a supported file under 25 MB with a title and category, and verify metadata, storage, project association, success feedback, and rejection of invalid files.

- [X] T013 [US1] Define upload metadata, validation result, and document summary DTOs in `ContosoDashboard/Services/DocumentContracts.cs`
- [X] T014 [US1] Implement `IDocumentService.UploadAsync` in `ContosoDashboard/Services/DocumentService.cs` with authenticated-user checks, metadata validation, extension/MIME and 25 MB validation, task-derived project authorization, rejection of missing-project tasks, and rejection of conflicting client project values
- [X] T015 [US1] Implement the one-document upload transaction in `ContosoDashboard/Services/DocumentService.cs` as validate, scan, generate GUID key, save file, persist metadata/activity atomically, and clean up temporary/stored content when persistence fails
- [X] T016 [US1] Add the protected single-document upload form with title, description, approved category, project/task selection, tags, file selection, explicit upload/scanning/storage progress states, and success/error feedback in `ContosoDashboard/Pages/Documents.razor`
- [X] T017 [US1] Implement the Blazor `InputFile` lifecycle in `ContosoDashboard/Pages/Documents.razor` with metadata captured before opening the stream, a 25 MB read limit, bounded progress reporting, cleared file reference, and `@key` reset
- [X] T018 [US1] Add employee navigation to the document page and protected route handling in `ContosoDashboard/Shared/NavMenu.razor` and `ContosoDashboard/Pages/Documents.razor`
- [X] T019 [US1] Add upload validation and authorization failure messages that never expose filesystem paths or internal exceptions in `ContosoDashboard/Pages/Documents.razor` and `ContosoDashboard/Services/DocumentService.cs`

**Checkpoint**: User Story 1 is independently usable as the MVP: a valid document can be uploaded, persisted securely, associated with a project/task, and rejected safely when invalid.

## Phase 4: User Story 2 - Find and Use Authorized Documents (Priority: P1)

**Goal**: Users can browse, filter, sort, search, preview, and download only documents they are authorized to access.

**Independent Test**: Seed documents across users/projects/shares, then verify authorized list/search results, filters, sorting, preview/download behavior, and denial without metadata leakage.

- [X] T020 [US2] Implement authorization-first document query composition for owner, administrator, project manager, project member, and active share recipient access in `ContosoDashboard/Services/DocumentAuthorization.cs`
- [X] T021 [US2] Implement paged list, sorting, category/project/date filters, and title/description/tag/uploader/project search in `ContosoDashboard/Services/DocumentService.cs`, applying predicates before materialization
- [X] T022 [US2] Add the document list UI with metadata columns, filter controls, search, sort selection, empty/loading/error states, and bounded result display in `ContosoDashboard/Pages/Documents.razor`
- [X] T023 [US2] Implement the protected `GET /documents/{documentId}/content` contract with authenticated user resolution, service authorization, inline PDF/JPEG/PNG preview, attachment download, safe disposition names, and non-leaking 401/403/404 behavior in `ContosoDashboard/Program.cs`
- [X] T024 [US2] Add preview and download actions to `ContosoDashboard/Pages/Documents.razor` using the protected content route and never exposing stored file paths
- [X] T025 [US2] Add database indexes and query limits needed to meet the up-to-500-document list/search target in `ContosoDashboard/Data/ApplicationDbContext.cs` and `ContosoDashboard/Services/DocumentService.cs`
- [X] T026 [US2] Add the content contract documentation and representative manual request checks to `specs/001-document-upload-management/contracts/document-content-api.md` and `specs/001-document-upload-management/quickstart.md`

**Checkpoint**: User Stories 1 and 2 work independently: users can upload documents and retrieve only authorized results/content.

## Phase 5: User Story 3 - Manage and Share Documents (Priority: P2)

**Goal**: Owners and authorized project managers can edit, replace, delete, and share documents with notification delivery.

**Independent Test**: Exercise owner, project-manager, employee, and administrator accounts through allowed and denied metadata, replacement, delete, and share operations.

- [X] T027 [US3] Implement metadata update, replacement, permanent delete, and share operations with owner/project-manager/administrator authorization in `ContosoDashboard/Services/DocumentService.cs`
- [X] T028 [US3] Implement replacement rollback and deletion cleanup so failed replacements preserve the old file and successful deletion removes metadata and binary content in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/LocalFileStorageService.cs`
- [X] T029 [US3] Persist active user/team shares with recipient validation and duplicate prevention in `ContosoDashboard/Models/Document.cs` and `ContosoDashboard/Data/ApplicationDbContext.cs`
- [X] T030 [US3] Create direct-user and department-share notifications through `INotificationService` in `ContosoDashboard/Services/DocumentService.cs`, notifying each eligible department user with in-app notifications enabled exactly once
- [X] T031 [US3] Add edit metadata, replace file, delete confirmation, direct-user/department recipient controls, and clear management feedback to `ContosoDashboard/Pages/Documents.razor`
- [X] T032 [US3] Add Shared with Me filtering and document access state to `ContosoDashboard/Pages/Documents.razor`
- [X] T033 [US3] Add clear management authorization, invalid recipient, replacement failure, and deletion feedback in `ContosoDashboard/Pages/Documents.razor`

**Checkpoint**: Owners and project managers can manage documents, recipients see shared documents and notifications, and unauthorized management attempts remain denied.

## Phase 6: User Story 4 - Use Documents in Project Work (Priority: P2)

**Goal**: Documents are available in project/task context and reflected in dashboard summaries.

**Independent Test**: Associate documents with projects and tasks, then verify project/task visibility, task upload context, member notifications, recent documents, and document counts.

- [X] T034 [US4] Include authorized project documents and document summaries in `ContosoDashboard/Services/ProjectService.cs` and `ContosoDashboard/Pages/ProjectDetails.razor`
- [X] T035 [US4] Add task document association and upload entry points while enforcing the task's project association in `ContosoDashboard/Services/TaskService.cs` and `ContosoDashboard/Pages/Tasks.razor`
- [X] T036 [US4] Add recent five uploads and document count fields to `ContosoDashboard/Services/DashboardService.cs` and render the widget/summary in `ContosoDashboard/Pages/Index.razor`
- [X] T037 [US4] Ensure project-document and department-share notifications are emitted once for eligible recipients and reuse existing notification preferences in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Services/NotificationService.cs`
- [X] T038 [US4] Add project/task/dashboard document empty, loading, access-denied, and success states in `ContosoDashboard/Pages/ProjectDetails.razor`, `ContosoDashboard/Pages/Tasks.razor`, and `ContosoDashboard/Pages/Index.razor`

**Checkpoint**: Project and task users can work with related documents in context, and the dashboard surfaces recent document activity.

## Phase 7: User Story 5 - Audit Document Activity (Priority: P3)

**Goal**: Administrators can review document activity and generate usage summaries while non-administrators are denied.

**Independent Test**: Perform upload, download, replacement, share, and delete actions as multiple users, then verify administrator activity/report results and non-administrator denial.

 [X] T039 [US5] Record upload, download, replacement, metadata update, share, and delete activities with actor, document, UTC time, and context; retain sanitized document ID/title JSON for deletes in `ContosoDashboard/Services/DocumentService.cs` and `ContosoDashboard/Models/Document.cs`
- [X] T041 [US5] Add the administrator-protected audit page with activity filters, report summaries, loading/error states, and denial handling in `ContosoDashboard/Pages/DocumentAudit.razor`
- [X] T042 [US5] Add administrator navigation and role enforcement for audit reporting in `ContosoDashboard/Shared/NavMenu.razor` and `ContosoDashboard/Pages/DocumentAudit.razor`

**Checkpoint**: Administrators can audit document activity and reporting; non-administrators cannot access audit data.

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validate security, performance, offline behavior, and documentation across all stories.

- [X] T043 [P] Add service-level authorization, validation, task-derived project, upload-cleanup, replacement-rollback, department-notification, and share tests in `ContosoDashboard.Tests/Services/DocumentServiceTests.cs`
- [X] T044 [P] Add storage path traversal, GUID naming, stream copy, delete, and scanner fail-closed tests in `ContosoDashboard.Tests/Services/FileStorageServiceTests.cs`
- [X] T045 [P] Add protected content endpoint tests for authorized access, unauthorized non-disclosure, department-share authorization, preview disposition, download disposition, and missing documents in `ContosoDashboard.Tests/Integration/DocumentContentEndpointTests.cs`
- [X] T046 Add EF migration or controlled schema initialization for document entities and indexes in `ContosoDashboard/Data/ApplicationDbContext.cs` and the repository database setup
- [ ] T047 Run the scenarios in `specs/001-document-upload-management/quickstart.md`, including one-file progress, task-derived project assignment, department notifications, retained delete audit identity, representative 500-document list/search timing, and 25 MB upload timing, and record deviations
- [X] T048 Run `dotnet build .\ContosoDashboard\` and the focused test project, then fix feature-related failures without changing unrelated behavior
- [X] T049 [P] Review `README.md` and `specs/001-document-upload-management/quickstart.md` for training-only mock authentication, scanner limitations, offline storage, reset instructions, and Azure migration boundaries
- [X] T050 Review all document routes and service methods for IDOR, path traversal, MIME spoofing, unauthorized metadata leakage, and missing audit events; record results in `specs/001-document-upload-management/checklists/security.md`

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies; T001-T004 can begin immediately.
- **Phase 2 Foundational**: Depends on Setup; T005-T012 block all user stories.
- **Phase 3 User Story 1**: Depends on Phase 2; delivers the MVP upload slice.
- **Phase 4 User Story 2**: Depends on Phase 2 and the document contracts from US1; it can begin after T013-T015 establish document upload contracts.
- **Phase 5 User Story 3**: Depends on US1 persistence and the US2 authorization query primitives.
- **Phase 6 User Story 4**: Depends on US1 document associations and existing project/task/dashboard services; it can proceed in parallel with US3 after the foundation.
- **Phase 7 User Story 5**: Depends on activity records from US1-US4.
- **Phase 8 Polish**: Depends on all desired stories and their integration points.

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational; standalone MVP.
- **US2 (P1)**: Depends on Foundational and the document contracts established by US1; retrieval can be independently demonstrated with seeded documents.
- **US3 (P2)**: Depends on US1 persistence and US2 authorization primitives.
- **US4 (P2)**: Depends on US1 associations and can run in parallel with US3 after those contracts exist.
- **US5 (P3)**: Depends on activity emission from prior stories.

### Parallel Execution Examples

- **Setup**: T002, T003, and T004 can run in parallel after T001 establishes configuration conventions.
- **Foundation**: T008, T009, T010, and T012 can run in parallel; T011 follows their service registrations.
- **US1**: T013 and the initial `Documents.razor` form work can proceed in parallel only when they touch separate files; T014-T015 follow the contracts.
- **US2**: T020, T023, and T025 can proceed in parallel after the service/model foundation; T022 and T024 follow the service contracts.
- **US3/US4**: After US1 and the shared authorization primitives, separate developers can work on T027-T033 and T034-T038 in parallel, coordinating on `DocumentService.cs`.
- **Polish**: T043-T045 and T049 can run in parallel; T047-T048 follow implementation completion.

## Implementation Strategy

### MVP First

1. Complete Phase 1 Setup and Phase 2 Foundational.
2. Complete Phase 3 User Story 1.
3. Run the upload, invalid-file, project-association, and cleanup checks from `quickstart.md`.
4. Stop and validate the upload MVP before starting sharing, dashboard, and audit work.

### Incremental Delivery

1. Add User Story 2 for authorized retrieval and search.
2. Add User Story 3 for metadata management and sharing.
3. Add User Story 4 for project/task/dashboard integration.
4. Add User Story 5 for administrator audit reporting.
5. Complete Phase 8 security, performance, build, and documentation validation.

### Notes

- Every task has a checkbox, sequential ID, required story label for story phases, and at least one concrete file path.
- `[P]` marks only tasks that can work on separate files without waiting on incomplete dependencies.
- Tests are validation-focused because the feature specification did not mandate TDD; the optional test project is included to make the security-sensitive behavior durable.
