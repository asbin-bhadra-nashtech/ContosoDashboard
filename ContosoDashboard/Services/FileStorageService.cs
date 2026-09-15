using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public sealed class DocumentStorageOptions
{
    public string RootPath { get; set; } = "AppData/uploads";
    public long MaxFileSizeBytes { get; set; } = DocumentValidation.MaxFileSizeBytes;
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream content, string userId, string? projectId, string fileName, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
}

public interface IFileScanner
{
    Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default);
}
