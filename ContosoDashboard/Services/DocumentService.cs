using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ContosoDashboard.Services;

public sealed class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IFileScanner _scanner;
    private readonly DocumentAuthorization _authorization;
    private readonly INotificationService _notifications;

    public DocumentService(ApplicationDbContext context, IFileStorageService storage, IFileScanner scanner, DocumentAuthorization authorization, INotificationService notifications)
    {
        _context = context;
        _storage = storage;
        _scanner = scanner;
        _authorization = authorization;
        _notifications = notifications;
    }

    public async Task<DocumentResult> UploadAsync(int requestingUserId, DocumentUploadRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateRequestAsync(requestingUserId, request, cancellationToken);
        if (validation != null) return DocumentResult.Failure(validation);

        if (request.Content.CanSeek) request.Content.Position = 0;
        if (!await _scanner.IsSafeAsync(request.Content, cancellationToken)) return DocumentResult.Failure("The file could not pass the safety scan.");
        if (request.Content.CanSeek) request.Content.Position = 0;

        string? relativePath = null;
        try
        {
            relativePath = await _storage.UploadAsync(request.Content, requestingUserId.ToString(), request.ProjectId?.ToString(), request.FileName, cancellationToken);
            var document = new Document
            {
                Title = request.Title.Trim(), Description = request.Description?.Trim(), Category = request.Category,
                Tags = request.Tags?.Trim(), OriginalFileName = DocumentValidation.GetSafeFileName(request.FileName),
                FilePath = relativePath, FileType = request.ContentType, FileSize = request.FileSize,
                UploadedByUserId = requestingUserId, ProjectId = request.ProjectId, TaskId = request.TaskId, UploadedDate = DateTime.UtcNow
            };
            _context.Documents.Add(document);
            await _context.SaveChangesAsync(cancellationToken);
            _context.DocumentActivities.Add(new DocumentActivity { DocumentId = document.DocumentId, UserId = requestingUserId, Action = "Upload", Details = document.OriginalFileName });
            await _context.SaveChangesAsync(cancellationToken);
            await NotifyProjectMembersAsync(document, requestingUserId, cancellationToken);
            return DocumentResult.Succeeded(document);
        }
        catch
        {
            if (relativePath != null) await _storage.DeleteAsync(relativePath, cancellationToken);
            return DocumentResult.Failure("The document could not be stored. Please try again.");
        }
    }

    public async Task<List<DocumentSummary>> SearchAsync(int requestingUserId, DocumentSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = _authorization.AccessibleDocuments(requestingUserId)
            .AsNoTracking()
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var search = request.SearchText.Trim();
            query = query.Where(d => d.Title.Contains(search) || (d.Description != null && d.Description.Contains(search)) || (d.Tags != null && d.Tags.Contains(search)) || d.UploadedByUser.DisplayName.Contains(search) || (d.Project != null && d.Project.Name.Contains(search)));
        }
        if (request.SharedWithMeOnly) query = query.Where(d => d.Shares.Any(s => s.IsActive));
        if (!string.IsNullOrWhiteSpace(request.Category)) query = query.Where(d => d.Category == request.Category);
        if (request.ProjectId.HasValue) query = query.Where(d => d.ProjectId == request.ProjectId);
        if (request.FromDate.HasValue) query = query.Where(d => d.UploadedDate >= request.FromDate.Value);
        if (request.ToDate.HasValue) query = query.Where(d => d.UploadedDate < request.ToDate.Value.Date.AddDays(1));
        query = request.SortBy.ToLowerInvariant() switch
        {
            "title" => query.OrderBy(d => d.Title),
            "category" => query.OrderBy(d => d.Category).ThenByDescending(d => d.UploadedDate),
            "size" => query.OrderByDescending(d => d.FileSize),
            _ => query.OrderByDescending(d => d.UploadedDate)
        };
        return await query.Take(500).Select(ToSummary()).ToListAsync(cancellationToken);
    }

    public async Task<DocumentContent?> GetContentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanReadAsync(document, requestingUserId, cancellationToken)) return null;
        var stream = await _storage.DownloadAsync(document.FilePath, cancellationToken);
        if (stream == null) return null;
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = requestingUserId, Action = "Download", Details = document.OriginalFileName });
        await _context.SaveChangesAsync(cancellationToken);
        return new DocumentContent { Content = stream, ContentType = document.FileType, FileName = DocumentValidation.GetSafeFileName(document.OriginalFileName) };
    }

    public Task<bool> CanManageAsync(Document document, int requestingUserId, CancellationToken cancellationToken = default) => _authorization.CanManageAsync(document, requestingUserId, cancellationToken);

    public async Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, string title, string? description, string category, string? tags, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, requestingUserId, cancellationToken) || !DocumentValidation.Categories.Contains(category)) return false;
        if (string.IsNullOrWhiteSpace(title)) return false;
        document.Title = title.Trim(); document.Description = description?.Trim(); document.Category = category; document.Tags = tags?.Trim();
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = requestingUserId, Action = "MetadataUpdate" });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DocumentResult> ReplaceAsync(int documentId, int requestingUserId, DocumentUploadRequest request, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, requestingUserId, cancellationToken)) return DocumentResult.Failure("You are not authorized to replace this document.");
        var validation = await ValidateRequestAsync(requestingUserId, request, cancellationToken);
        if (validation != null) return DocumentResult.Failure(validation);
        if (request.Content.CanSeek) request.Content.Position = 0;
        if (!await _scanner.IsSafeAsync(request.Content, cancellationToken)) return DocumentResult.Failure("The file could not pass the safety scan.");
        if (request.Content.CanSeek) request.Content.Position = 0;
        var oldPath = document.FilePath;
        var oldFileName = document.OriginalFileName;
        var oldFileType = document.FileType;
        var oldFileSize = document.FileSize;
        string? newPath = null;
        try
        {
            newPath = await _storage.UploadAsync(request.Content, requestingUserId.ToString(), document.ProjectId?.ToString(), request.FileName, cancellationToken);
            document.FilePath = newPath;
            document.OriginalFileName = DocumentValidation.GetSafeFileName(request.FileName);
            document.FileType = request.ContentType;
            document.FileSize = request.FileSize;
            _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = requestingUserId, Action = "Replace" });
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            document.FilePath = oldPath;
            document.OriginalFileName = oldFileName;
            document.FileType = oldFileType;
            document.FileSize = oldFileSize;
            _context.Entry(document).State = EntityState.Unchanged;
            if (newPath != null) await _storage.DeleteAsync(newPath, CancellationToken.None);
            return DocumentResult.Failure("The replacement could not be stored. The existing document is unchanged.");
        }

        try
        {
            await _storage.DeleteAsync(oldPath, cancellationToken);
        }
        catch
        {
            return DocumentResult.Failure("The replacement was saved, but the previous file could not be cleaned up.");
        }

        return DocumentResult.Succeeded(document);
    }

    public async Task<bool> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, requestingUserId, cancellationToken)) return false;
        var path = document.FilePath;
        try
        {
            await _storage.DeleteAsync(path, cancellationToken);
            _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = requestingUserId, Action = "Delete", Details = document.OriginalFileName });
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DocumentResult> ShareAsync(int documentId, int requestingUserId, int? userId, string? department, CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(d => d.DocumentId == documentId, cancellationToken);
        if (document == null || !await _authorization.CanManageAsync(document, requestingUserId, cancellationToken)) return DocumentResult.Failure("You are not authorized to share this document.");
        if (!userId.HasValue && string.IsNullOrWhiteSpace(department)) return DocumentResult.Failure("Choose a user or department.");
        if (userId.HasValue && !await _context.Users.AnyAsync(u => u.UserId == userId && u.InAppNotificationsEnabled, cancellationToken)) return DocumentResult.Failure("The selected user is unavailable.");
        var normalizedDepartment = department?.Trim();
        var duplicateShare = await _context.DocumentShares.AnyAsync(s => s.DocumentId == documentId && s.IsActive && s.SharedWithUserId == userId && s.SharedWithDepartment == normalizedDepartment, cancellationToken);
        if (duplicateShare) return DocumentResult.Failure("This document is already shared with that recipient.");
        var share = new DocumentShare { DocumentId = documentId, SharedByUserId = requestingUserId, SharedWithUserId = userId, SharedWithDepartment = normalizedDepartment };
        _context.DocumentShares.Add(share);
        _context.DocumentActivities.Add(new DocumentActivity { DocumentId = documentId, UserId = requestingUserId, Action = "Share", Details = userId?.ToString() ?? department });
        await _context.SaveChangesAsync(cancellationToken);
        if (userId.HasValue) await _notifications.CreateNotificationAsync(new Notification { UserId = userId.Value, Title = "Document shared with you", Message = $"{document.Title} is now available in Shared with Me.", Type = NotificationType.SystemAnnouncement, Priority = NotificationPriority.Informational });
        return DocumentResult.Succeeded(document);
    }

    public async Task<List<DocumentSummary>> GetRecentAsync(int requestingUserId, int count, CancellationToken cancellationToken = default) => await SearchAsync(requestingUserId, new DocumentSearchRequest { SortBy = "date" }, cancellationToken).ContinueWith(t => t.Result.Take(Math.Clamp(count, 1, 20)).ToList(), cancellationToken);

    public Task<int> CountAsync(int requestingUserId, CancellationToken cancellationToken = default) => _authorization.AccessibleDocuments(requestingUserId).CountAsync(cancellationToken);

    private async Task<string?> ValidateRequestAsync(int userId, DocumentUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.Content == Stream.Null || request.FileSize <= 0 || request.FileSize > DocumentValidation.MaxFileSizeBytes) return "Files must be between 1 byte and 25 MB.";
        if (string.IsNullOrWhiteSpace(request.Title)) return "A document title is required.";
        if (!DocumentValidation.Categories.Contains(request.Category)) return "Choose a valid document category.";
        if (!DocumentValidation.IsAllowed(request.FileName, request.ContentType)) return "This file type is not supported.";
        if (request.ProjectId.HasValue && !await _context.Projects.AnyAsync(p => p.ProjectId == request.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)), cancellationToken)) return "You are not a member of the selected project.";
        if (request.TaskId.HasValue)
        {
            var task = await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.TaskId == request.TaskId, cancellationToken);
            if (task == null || task.ProjectId != request.ProjectId) return "The selected task does not belong to the selected project.";
        }
        return null;
    }

    private static Expression<Func<Document, DocumentSummary>> ToSummary() => d => new DocumentSummary
    {
        DocumentId = d.DocumentId, Title = d.Title, Description = d.Description, Category = d.Category, Tags = d.Tags,
        OriginalFileName = d.OriginalFileName, FileType = d.FileType, FileSize = d.FileSize, UploadedDate = d.UploadedDate,
        UploaderName = d.UploadedByUser.DisplayName, ProjectName = d.Project != null ? d.Project.Name : null, ProjectId = d.ProjectId, TaskId = d.TaskId
        , UploadedByUserId = d.UploadedByUserId
    };

    private async Task NotifyProjectMembersAsync(Document document, int uploaderId, CancellationToken cancellationToken)
    {
        if (!document.ProjectId.HasValue) return;
        var managerId = await _context.Projects.Where(p => p.ProjectId == document.ProjectId).Select(p => (int?)p.ProjectManagerId).FirstOrDefaultAsync(cancellationToken);
        var recipients = await _context.ProjectMembers.Where(pm => pm.ProjectId == document.ProjectId && pm.UserId != uploaderId).Select(pm => pm.UserId).ToListAsync(cancellationToken);
        if (managerId.HasValue && managerId.Value != uploaderId) recipients.Add(managerId.Value);
        recipients = recipients.Distinct().ToList();
        var enabledRecipients = await _context.Users.Where(u => recipients.Contains(u.UserId) && u.InAppNotificationsEnabled).Select(u => u.UserId).ToListAsync(cancellationToken);
        foreach (var userId in enabledRecipients) await _notifications.CreateNotificationAsync(new Notification { UserId = userId, Title = "New project document", Message = $"A new document was added: {document.Title}", Type = NotificationType.ProjectUpdate, Priority = NotificationPriority.Informational });
    }
}
