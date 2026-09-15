using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public sealed class DocumentAuthorization
{
    private readonly ApplicationDbContext _context;

    public DocumentAuthorization(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<Document> AccessibleDocuments(int userId)
    {
        var user = _context.Users.AsNoTracking().FirstOrDefault(u => u.UserId == userId);
        if (user?.Role == UserRole.Administrator)
            return _context.Documents;

        return _context.Documents.Where(d =>
            d.UploadedByUserId == userId
            || (d.ProjectId.HasValue && _context.Projects.Any(p => p.ProjectId == d.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId))))
            || d.Shares.Any(s => s.IsActive && (s.SharedWithUserId == userId || (s.SharedWithDepartment != null && s.SharedWithDepartment == user!.Department))));
    }

    public async Task<bool> CanReadAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return false;
        if (user.Role == UserRole.Administrator || document.UploadedByUserId == userId) return true;
        if (document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)), cancellationToken)) return true;
        return await _context.DocumentShares.AnyAsync(s => s.DocumentId == document.DocumentId && s.IsActive && (s.SharedWithUserId == userId || (s.SharedWithDepartment != null && s.SharedWithDepartment == user.Department)), cancellationToken);
    }

    public async Task<bool> CanManageAsync(Document document, int userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user == null) return false;
        if (user.Role == UserRole.Administrator || document.UploadedByUserId == userId) return true;
        return document.ProjectId.HasValue && await _context.Projects.AnyAsync(p => p.ProjectId == document.ProjectId && p.ProjectManagerId == userId, cancellationToken);
    }
}
