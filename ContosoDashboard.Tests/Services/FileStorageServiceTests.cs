using ContosoDashboard.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContosoDashboard.Tests.Services;

public sealed class FileStorageServiceTests
{
    [Fact]
    public async Task LocalStorageCopiesContentAndUsesGeneratedRelativeKey()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new LocalFileStorageService(
                Options.Create(new DocumentStorageOptions { RootPath = root }),
                new TestWebHostEnvironment(root));
            await using var input = new MemoryStream("document"u8.ToArray());

            var relativePath = await storage.UploadAsync(input, "4", "1", "report.pdf");
            var downloaded = await storage.DownloadAsync(relativePath);

            Assert.NotNull(downloaded);
            Assert.Matches("^4/1/[0-9a-f]{32}\\.pdf$", relativePath);
            using var reader = new StreamReader(downloaded!);
            Assert.Equal("document", await reader.ReadToEndAsync());
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Fact]
    public async Task LocalStorageRejectsPathTraversal()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storage = new LocalFileStorageService(
            Options.Create(new DocumentStorageOptions { RootPath = root }),
            new TestWebHostEnvironment(root));

        await Assert.ThrowsAsync<InvalidOperationException>(() => storage.DownloadAsync("../outside.txt"));
        Directory.Delete(root, true);
    }

    [Fact]
    public async Task ScannerRejectsUnavailableAndUnsafeContent()
    {
        var scanner = new LocalFileScanner();
        Assert.False(await scanner.IsSafeAsync(Stream.Null));
        await using var unsafeContent = new MemoryStream("EICAR test signature"u8.ToArray());
        Assert.False(await scanner.IsSafeAsync(unsafeContent));
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string root) => ContentRootPath = root;
        public string ApplicationName { get; set; } = "ContosoDashboard.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string EnvironmentName { get; set; } = "Development";
    }
}
