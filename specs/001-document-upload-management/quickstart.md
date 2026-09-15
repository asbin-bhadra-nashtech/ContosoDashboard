# Quickstart Validation: Document Upload and Management

## Prerequisites

- .NET 8 SDK
- SQL Server LocalDB (`MSSQLLocalDB`)
- PowerShell
- A browser
- Repository root as the current directory

The application uses mock authentication for training. Use the seeded users described in the repository README. This feature remains offline-capable; no cloud account or external service is required.

## Start the application

```powershell
dotnet build .\ContosoDashboard\
dotnet run --project .\ContosoDashboard\
```

Open the HTTPS URL printed by the application and sign in at `/login`.

## Scenario 1: Upload validation and metadata

1. Sign in as Ni Kang (Employee).
2. Open the document management page and choose a supported PDF or image under 25 MB.
3. Enter a title and one approved category; optionally select the seeded project or a task and add tags. A selected task supplies its project automatically.
4. Submit the upload.
5. Verify progress, success feedback, metadata, and the new document in My Documents.
6. Repeat with a file over 25 MB and an unsupported extension; verify both fail without a document record or public file.
7. Configure or exercise the local scanner's deterministic unsafe-file test case; verify an unsafe or unavailable scan blocks publication.

Expected result: valid documents are stored outside `wwwroot`, receive a generated storage key, and invalid documents are rejected with actionable messages.

## Scenario 2: Authorization and retrieval

1. As Ni Kang, verify the uploaded document appears in My Documents.
2. As an authorized member of the associated project, verify project documents are visible and downloadable.
3. As a user without ownership, project membership, administrator access, or an active share, request the document list, search, preview, and content route.
4. Verify the unauthorized user receives no document metadata or content.
5. Search by title, description, tag, uploader, and project; verify category/project/date filters and title/date/category/size sorting.
6. Preview a PDF/image and download a document; verify the content type and disposition are correct.

Expected result: authorization is applied before data is returned and representative search/list actions meet the 2-second target for up to 500 documents.

## Scenario 3: Management, sharing, and notifications

1. As the owner, edit metadata and replace the file with a valid supported file.
2. Attempt replacement with an invalid file and verify the previous file remains available.
3. Share the document with a seeded user or department. For a department share, verify every eligible active department user with in-app notifications enabled receives one notification.
4. Sign in as a recipient and verify Shared with Me and access independent of notification preference.
5. As the project manager, manage a project document; as an employee who is neither owner nor manager, verify delete is denied.
6. Delete as the owner after confirmation and verify the metadata, file, search result, and access route are removed.

Expected result: management and sharing follow the authorization matrix and failed operations do not leave inconsistent records or files.

## Scenario 4: Project, task, dashboard, and audit integration

1. Associate a document with a task; verify the task's project is assigned automatically. Attempt a task without a project and verify rejection.
2. Open the project and task views as an authorized user and verify the document attachment is visible.
3. Add a project document and verify eligible members receive notifications.
4. Open the dashboard and verify Recent Documents contains the five latest uploads and the document count is correct.
5. As an administrator, verify activity entries for upload, download, replacement, share, and delete and generate the summary report.
6. As a non-administrator, verify audit data is denied.
7. After deletion, as an administrator verify the audit event retains the sanitized document ID/title details while the live document relationship is absent.

Expected result: all integration points use the existing user, project, task, notification, and dashboard boundaries.

## Automated checks

```powershell
dotnet build .\ContosoDashboard\
dotnet test .\ContosoDashboard.Tests\ContosoDashboard.Tests.csproj
```

Add focused service and endpoint tests during implementation for validation, authorization/IDOR, upload cleanup, replacement rollback, search filtering, and audit creation. Run the build and those tests before considering the feature complete.

## Data reset

For repeatable manual runs, reset the LocalDB database using the repository's documented training procedure, then start the application to recreate seeded data. Do not use production data for these checks.
