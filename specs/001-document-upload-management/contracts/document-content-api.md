# Document Content Contract

## Purpose

Define the protected HTTP contract used by browser downloads and PDF/image previews. Blazor pages and the service layer remain the primary application interface; this endpoint exists because binary content must be streamed through an authorization boundary.

## Route

`GET /documents/{documentId}/content`

### Parameters

- `documentId`: positive integer document identifier.
- `download` (optional query parameter): `true` requests attachment disposition; omitted or `false` permits inline preview for supported preview types.

### Authentication

The request MUST be authenticated with the existing cookie authentication scheme. Unauthenticated requests are redirected or rejected according to the application's existing authorization configuration.

### Authorization

The endpoint MUST call the document service with the current user's integer ID. Access is granted only when the user is the owner, an administrator, a project manager for the associated project, an authorized project member, or an active explicit share recipient.

### Success: `200 OK`

- Body: file stream from the storage abstraction.
- `Content-Type`: persisted validated MIME type.
- `Content-Disposition`: `inline` for supported PDF/JPEG/PNG previews when `download` is false; `attachment` otherwise.
- `Content-Length`: persisted file size when available.
- The endpoint MUST use the stored relative storage key, never a client-supplied path or original filename.
- Department-based shares MUST authorize users by their current `User.Department` value; notification preference does not affect content authorization.

### Failure responses

- `401 Unauthorized`: no authenticated user.
- `403 Forbidden`: authenticated user lacks document access.
- `404 Not Found`: document does not exist, has been permanently deleted, or is unavailable. The response MUST not reveal whether an unauthorized document exists.
- `410 Gone`: optional response when metadata remains for a documented audit-retention reason but content is permanently unavailable.
- `500 Internal Server Error`: only for unexpected failures; do not expose filesystem paths or exception details.

## Related service contract

`IDocumentService` MUST expose equivalent authorization-aware operations for list/search, upload, metadata update, replacement, delete, share, and audit reporting. Every operation accepts the requesting user identity or an equivalent authenticated context and returns no unauthorized document data.

## Security requirements

- Validate the route identifier as an integer.
- Apply authorization before opening the stream.
- Do not map the upload root as static content.
- Return safe content-disposition filenames derived from sanitized original metadata, never filesystem paths.
- Record successful downloads as `DocumentActivity` after authorization.
- Delete activity is retained after document deletion with a nullable document relationship and sanitized identifier/title details.
