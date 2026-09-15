using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoDashboard.Tests.Services;

public sealed class DocumentServiceTests
{
    [Fact]
    public async Task UploadRejectsUnsupportedFilesBeforeStorage()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { UserId = 4, DisplayName = "Employee", Email = "employee@test.local", InAppNotificationsEnabled = true });
        await context.SaveChangesAsync();
        var storage = new TestStorage();
        var service = CreateService(context, storage, new TestScanner(true));

        var result = await service.UploadAsync(4, Request("file.exe", "application/octet-stream"));

        Assert.False(result.Success);
        Assert.Empty(storage.UploadedPaths);
    }

    [Fact]
    public async Task UploadRejectsUnsafeFilesBeforeStorage()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { UserId = 4, DisplayName = "Employee", Email = "employee@test.local", InAppNotificationsEnabled = true });
        await context.SaveChangesAsync();
        var storage = new TestStorage();
        var service = CreateService(context, storage, new TestScanner(false));

        var result = await service.UploadAsync(4, Request("file.pdf", "application/pdf"));

        Assert.False(result.Success);
        Assert.Empty(storage.UploadedPaths);
    }

    [Fact]
    public async Task UploadDerivesProjectFromTask()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { UserId = 4, DisplayName = "Employee", Email = "employee@test.local", Department = "Engineering", InAppNotificationsEnabled = true });
        context.Projects.Add(new Project { ProjectId = 1, Name = "Project", ProjectManagerId = 4 });
        context.Tasks.Add(new TaskItem { TaskId = 7, Title = "Task", ProjectId = 1, AssignedUserId = 4, CreatedByUserId = 4 });
        await context.SaveChangesAsync();
        var storage = new TestStorage();
        var service = CreateService(context, storage, new TestScanner(true));

        var result = await service.UploadAsync(4, Request("file.pdf", "application/pdf", taskId: 7));

        Assert.True(result.Success);
        Assert.Equal(1, result.Document!.ProjectId);
        Assert.Equal("1", storage.ProjectIds.Single());
    }

    [Fact]
    public async Task DepartmentShareNotifiesEligibleUsersWithoutBlockingDisabledNotifications()
    {
        await using var context = CreateContext();
        context.Users.AddRange(
            new User { UserId = 4, DisplayName = "Owner", Email = "owner@test.local", Department = "Engineering", InAppNotificationsEnabled = true },
            new User { UserId = 5, DisplayName = "Enabled", Email = "enabled@test.local", Department = "Engineering", InAppNotificationsEnabled = true },
            new User { UserId = 6, DisplayName = "Disabled", Email = "disabled@test.local", Department = "Engineering", InAppNotificationsEnabled = false });
        var document = new Document { DocumentId = 10, Title = "Shared", Category = "Other", OriginalFileName = "shared.pdf", FilePath = "4/personal/file.pdf", FileType = "application/pdf", FileSize = 4, UploadedByUserId = 4 };
        context.Documents.Add(document);
        await context.SaveChangesAsync();
        var notifications = new TestNotifications();
        var service = new DocumentService(context, new TestStorage(), new TestScanner(true), new DocumentAuthorization(context), notifications);

        var result = await service.ShareAsync(10, 4, null, "Engineering");

        Assert.True(result.Success);
        Assert.Single(notifications.Created.Where(notification => notification.UserId == 5));
        Assert.DoesNotContain(notifications.Created, notification => notification.UserId == 6);
        Assert.True(await new DocumentAuthorization(context).CanReadAsync(document, 6));
    }

    [Fact]
    public async Task DeleteRetainsSanitizedDocumentIdentityInAuditActivity()
    {
        await using var context = CreateContext();
        context.Users.Add(new User { UserId = 4, DisplayName = "Owner", Email = "owner@test.local", InAppNotificationsEnabled = true });
        context.Documents.Add(new Document { DocumentId = 11, Title = "Quarter\nly Report", Category = "Reports", OriginalFileName = "report.pdf", FilePath = "4/personal/report.pdf", FileType = "application/pdf", FileSize = 4, UploadedByUserId = 4 });
        await context.SaveChangesAsync();
        var service = CreateService(context, new TestStorage(), new TestScanner(true));

        Assert.True(await service.DeleteAsync(11, 4));
        var activity = await context.DocumentActivities.SingleAsync(a => a.Action == "Delete");
        Assert.Null(activity.DocumentId);
        Assert.Contains("\"documentId\":11", activity.Details);
        Assert.Contains("Quarterly Report", activity.Details);
    }

    private static DocumentService CreateService(ApplicationDbContext context, TestStorage storage, TestScanner scanner) =>
        new(context, storage, scanner, new DocumentAuthorization(context), new TestNotifications());

    private static DocumentUploadRequest Request(string name, string contentType, int? taskId = null, int? projectId = null) => new()
    {
        Title = "Test document",
        Category = "Other",
        FileName = name,
        ContentType = contentType,
        FileSize = 4,
        TaskId = taskId,
        ProjectId = projectId,
        Content = new MemoryStream("test"u8.ToArray())
    };

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private sealed class TestScanner(bool result) : IFileScanner
    {
        public Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class TestStorage : IFileStorageService
    {
        public List<string> UploadedPaths { get; } = new();
        public List<string?> ProjectIds { get; } = new();
        public Task<string> UploadAsync(Stream content, string userId, string? projectId, string fileName, CancellationToken cancellationToken = default)
        {
            var path = $"{userId}/test/file.pdf";
            UploadedPaths.Add(path);
            ProjectIds.Add(projectId);
            return Task.FromResult(path);
        }
        public Task<Stream?> DownloadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestNotifications : INotificationService
    {
        public List<Notification> Created { get; } = new();
        public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>());
        public Task<Notification> CreateNotificationAsync(Notification notification) { Created.Add(notification); return Task.FromResult(notification); }
        public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false);
        public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0);
    }
}
