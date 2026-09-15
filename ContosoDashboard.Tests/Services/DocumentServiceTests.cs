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

    private static DocumentService CreateService(ApplicationDbContext context, TestStorage storage, TestScanner scanner) =>
        new(context, storage, scanner, new DocumentAuthorization(context), new TestNotifications());

    private static DocumentUploadRequest Request(string name, string contentType) => new()
    {
        Title = "Test document",
        Category = "Other",
        FileName = name,
        ContentType = contentType,
        FileSize = 4,
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
        public Task<string> UploadAsync(Stream content, string userId, string? projectId, string fileName, CancellationToken cancellationToken = default)
        {
            var path = $"{userId}/test/file.pdf";
            UploadedPaths.Add(path);
            return Task.FromResult(path);
        }
        public Task<Stream?> DownloadAsync(string relativePath, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
        public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestNotifications : INotificationService
    {
        public Task<List<Notification>> GetUserNotificationsAsync(int userId, bool unreadOnly = false) => Task.FromResult(new List<Notification>());
        public Task<Notification> CreateNotificationAsync(Notification notification) => Task.FromResult(notification);
        public Task<bool> MarkAsReadAsync(int notificationId, int requestingUserId) => Task.FromResult(false);
        public Task<int> GetUnreadCountAsync(int userId) => Task.FromResult(0);
    }
}
