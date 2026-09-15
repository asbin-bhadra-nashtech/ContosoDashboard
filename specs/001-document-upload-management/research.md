# Research: Document Upload and Management

## Decision: Extend the existing single-project layered architecture

**Rationale**: The repository is an ASP.NET Core 8.0 Blazor Server application with EF Core SQL Server LocalDB, models in `ContosoDashboard/Models`, persistence in `Data/ApplicationDbContext.cs`, business services in `Services`, and protected Razor components in `Pages`. Adding a separate project or API service would violate the constitution's simple, training-first scope and is unnecessary for the feature.

**Alternatives considered**: A separate document microservice or frontend/backend split was rejected because the application is offline-first, has no existing service boundary, and the feature can follow established service and component patterns.

## Decision: Store binaries outside `wwwroot` behind `IFileStorageService`

**Rationale**: Files under `wwwroot` would be publicly addressable and bypass service authorization. A relative storage key such as `{userId}/{projectId-or-personal}/{guid}.{extension}` keeps paths portable for a future blob implementation while the local implementation uses a configured `AppData/uploads` root. The upload transaction is ordered as validate, generate key, save file, then persist metadata; failed metadata persistence must remove the saved file.

**Alternatives considered**: Storing user filenames or files directly in `wwwroot` was rejected because it creates path traversal, collision, and unauthorized-download risks. Storing binary content in SQL Server was rejected because the stakeholder requirements explicitly call for local filesystem storage and cloud-storage migration readiness.

## Decision: Use an integer `DocumentId` and text category values

**Rationale**: This follows the explicit data constraints and existing integer primary keys. Categories remain approved text values so the database is easy to inspect during training and avoids an enum migration contract.

**Alternatives considered**: GUID document keys and integer enum categories were rejected by the stakeholder requirements.

## Decision: Put authorization in `DocumentService` for every document operation

**Rationale**: Existing services accept a requesting user ID and return an empty/null result or failure when ownership or project membership is invalid. The document service will centralize owner, project-manager, project-member, share-recipient, and administrator rules before querying content or metadata. This provides IDOR protection for both Blazor UI calls and binary endpoints.

**Alternatives considered**: Relying only on `[Authorize]` at the page or endpoint was rejected because authentication alone cannot distinguish document ownership, project membership, and explicit shares.

## Decision: Serve downloads and previews through authorized minimal HTTP endpoints

**Rationale**: Blazor components can request a route such as `/documents/{id}/content` while the endpoint asks `IDocumentService` for an authorized stream and content type. The endpoint must never accept a client-supplied filesystem path and must use the stored relative key only after authorization. The same route can support inline preview for PDF/images and attachment disposition for downloads.

**Alternatives considered**: Static file mapping was rejected because it would make authorization difficult or impossible. Embedding large files in component state was rejected because it increases memory pressure and is not appropriate for 25 MB uploads.

## Decision: Use a storage abstraction plus a scanner abstraction

**Rationale**: `IFileStorageService` preserves the local-to-Azure migration boundary required by the stakeholder document. `IFileScanner` separates the malware-scan policy from storage and business rules. The offline training implementation must fail closed when scanning is unavailable and must document that a production deployment requires a real antivirus engine; the training implementation may provide deterministic test signatures for repeatable acceptance tests but must not claim production malware protection.

**Alternatives considered**: Direct `System.IO` calls in components were rejected because they mix UI, authorization, and infrastructure. A hard dependency on an online scanning service was rejected because core workflows must work offline.

## Decision: Use explicit operation records for audit activity

**Rationale**: Upload, download, delete, and share are security-relevant actions and need actor, document, action, timestamp, and context. A dedicated `DocumentActivity` entity allows administrator reporting without coupling reports to notification records.

**Alternatives considered**: Application logs alone were rejected because they are not queryable as user-facing reports and are not durable domain data. Reusing notifications was rejected because notifications are recipient-oriented rather than audit-oriented.

## Decision: Validate list/search performance with indexed, authorization-first queries

**Rationale**: Up to 500 documents is modest for LocalDB, but authorization predicates must be applied in the database query before sorting and paging. Indexes should cover document owner, project, upload date, category, and share lookups. Search will use bounded string predicates over title, description, tags, uploader, and project name; the quickstart will measure representative results against the stated 2-second goal.

**Alternatives considered**: Loading every document into memory before filtering was rejected because it risks leaking unauthorized data through intermediate state and does not scale to the stated list/search target.
