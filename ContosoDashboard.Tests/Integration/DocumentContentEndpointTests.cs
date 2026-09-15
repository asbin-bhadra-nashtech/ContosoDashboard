using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Net;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ContosoDashboard.Tests.Integration;

public sealed class DocumentContentEndpointTests : IClassFixture<DocumentContentFactory>
{
    private readonly HttpClient client;

    public DocumentContentEndpointTests(DocumentContentFactory factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task AuthorizedPreviewUsesInlineDisposition()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/documents/10/content");
        request.Headers.Add("X-Test-User", "4");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("inline", response.Content.Headers.ContentDisposition?.DispositionType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthorizedDownloadUsesAttachmentDisposition()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/documents/10/content?download=true");
        request.Headers.Add("X-Test-User", "4");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("attachment", response.Content.Headers.ContentDisposition?.DispositionType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnauthorizedRequestDoesNotReturnContent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/documents/10/content");
        request.Headers.Add("X-Test-User", "6");

        using var response = await client.SendAsync(request);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("document body", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task DepartmentShareRecipientCanReadContent()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/documents/10/content");
        request.Headers.Add("X-Test-User", "5");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

public sealed class DocumentContentFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("DocumentContentEndpointTests"));
            services.RemoveAll<IDocumentService>();
            services.AddScoped<IDocumentService, TestDocumentService>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme;
                options.DefaultChallengeScheme = TestAuthenticationHandler.Scheme;
            }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.Scheme, _ => { });
        });
    }
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public new const string Scheme = "DocumentEndpointTest";

    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-User", out var value) || !int.TryParse(value, out var userId)) return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()), new Claim(ClaimTypes.Name, $"User {userId}"), new Claim(ClaimTypes.Role, "Employee") }, Scheme);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme)));
    }
}

public sealed class TestDocumentService : IDocumentService
{
    public Task<DocumentResult> UploadAsync(int requestingUserId, DocumentUploadRequest request, CancellationToken cancellationToken = default) => Task.FromResult(DocumentResult.Failure("Not used"));
    public Task<List<DocumentSummary>> SearchAsync(int requestingUserId, DocumentSearchRequest request, CancellationToken cancellationToken = default) => Task.FromResult(new List<DocumentSummary>());
    public Task<DocumentContent?> GetContentAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default)
    {
        if (documentId != 10 || (requestingUserId != 4 && requestingUserId != 5)) return Task.FromResult<DocumentContent?>(null);
        return Task.FromResult<DocumentContent?>(new DocumentContent { Content = new MemoryStream("document body"u8.ToArray()), ContentType = "application/pdf", FileName = "report.pdf" });
    }
    public Task<bool> CanManageAsync(Document document, int requestingUserId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, string title, string? description, string category, string? tags, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<DocumentResult> ReplaceAsync(int documentId, int requestingUserId, DocumentUploadRequest request, CancellationToken cancellationToken = default) => Task.FromResult(DocumentResult.Failure("Not used"));
    public Task<bool> DeleteAsync(int documentId, int requestingUserId, CancellationToken cancellationToken = default) => Task.FromResult(false);
    public Task<DocumentResult> ShareAsync(int documentId, int requestingUserId, int? userId, string? department, CancellationToken cancellationToken = default) => Task.FromResult(DocumentResult.Failure("Not used"));
    public Task<List<DocumentSummary>> GetRecentAsync(int requestingUserId, int count, CancellationToken cancellationToken = default) => Task.FromResult(new List<DocumentSummary>());
    public Task<int> CountAsync(int requestingUserId, CancellationToken cancellationToken = default) => Task.FromResult(0);
}
