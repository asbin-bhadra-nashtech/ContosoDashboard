using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public sealed class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
    public Stream Content { get; set; } = Stream.Null;
}

public sealed class DocumentSearchRequest
{
    public string? SearchText { get; set; }
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool SharedWithMeOnly { get; set; }
    public string SortBy { get; set; } = "date";
}

public sealed class DocumentSummary
{
    public int DocumentId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = string.Empty;
    public string? Tags { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string FileType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public DateTime UploadedDate { get; init; }
    public string UploaderName { get; init; } = string.Empty;
    public int UploadedByUserId { get; init; }
    public string? ProjectName { get; init; }
    public int? ProjectId { get; init; }
    public int? TaskId { get; init; }
}

public sealed class DocumentContent
{
    public Stream Content { get; init; } = Stream.Null;
    public string ContentType { get; init; } = "application/octet-stream";
    public string FileName { get; init; } = "download";
}

public sealed record DocumentResult(bool Success, string? Error, Document? Document)
{
    public static DocumentResult Failure(string error) => new(false, error, null);
    public static DocumentResult Succeeded(Document document) => new(true, null, document);
}

public interface IDocumentService
{
    Task<DocumentResult> UploadAsync(int requestingUserId, DocumentUploadRequest request, CancellationToken cancellationToken = default);
    Task<List<DocumentSummary>> SearchAsync(int requestingUserId, DocumentSearchRequest request, CancellationToken cancellationToken = default);
    Task<DocumentContent?> GetContentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> CanManageAsync(Document document, int requestingUserId, CancellationToken cancellationToken = default);
    Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, string title, string? description, string category, string? tags, CancellationToken cancellationToken = default);
    Task<DocumentResult> ReplaceAsync(int documentId, int requestingUserId, DocumentUploadRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default);
    Task<DocumentResult> ShareAsync(int documentId, int requestingUserId, int? userId, string? department, CancellationToken cancellationToken = default);
    Task<List<DocumentSummary>> GetRecentAsync(int requestingUserId, int count, CancellationToken cancellationToken = default);
    Task<int> CountAsync(int requestingUserId, CancellationToken cancellationToken = default);
}
