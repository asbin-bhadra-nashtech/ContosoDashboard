using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentAuditService
{
    private readonly ApplicationDbContext _context;

    public DocumentAuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DocumentActivity>> GetActivitiesAsync(int requestingUserId, string? action = null, CancellationToken cancellationToken = default)
    {
        if (!await IsAdministratorAsync(requestingUserId, cancellationToken)) return new List<DocumentActivity>();
        var query = _context.DocumentActivities.AsNoTracking().Include(a => a.User).Include(a => a.Document).AsQueryable();
        if (!string.IsNullOrWhiteSpace(action)) query = query.Where(a => a.Action == action);
        return await query.OrderByDescending(a => a.OccurredDate).Take(500).ToListAsync(cancellationToken);
    }

    public async Task<DocumentAuditReport?> GetReportAsync(int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (!await IsAdministratorAsync(requestingUserId, cancellationToken)) return null;
        return new DocumentAuditReport
        {
            TotalDocuments = await _context.Documents.CountAsync(cancellationToken),
            MostUploadedTypes = await _context.Documents.GroupBy(d => d.FileType).Select(g => new DocumentMetric(g.Key, g.Count())).OrderByDescending(m => m.Count).Take(5).ToListAsync(cancellationToken),
            MostActiveUploaders = await _context.Documents.GroupBy(d => d.UploadedByUser.DisplayName).Select(g => new DocumentMetric(g.Key, g.Count())).OrderByDescending(m => m.Count).Take(5).ToListAsync(cancellationToken),
            AccessPatterns = await _context.DocumentActivities.GroupBy(a => a.Action).Select(g => new DocumentMetric(g.Key, g.Count())).OrderByDescending(m => m.Count).ToListAsync(cancellationToken)
        };
    }

    private Task<bool> IsAdministratorAsync(int userId, CancellationToken cancellationToken) => _context.Users.AnyAsync(u => u.UserId == userId && u.Role == UserRole.Administrator, cancellationToken);
}

public sealed record DocumentMetric(string Name, int Count);

public sealed class DocumentAuditReport
{
    public int TotalDocuments { get; init; }
    public List<DocumentMetric> MostUploadedTypes { get; init; } = new();
    public List<DocumentMetric> MostActiveUploaders { get; init; } = new();
    public List<DocumentMetric> AccessPatterns { get; init; } = new();
}
