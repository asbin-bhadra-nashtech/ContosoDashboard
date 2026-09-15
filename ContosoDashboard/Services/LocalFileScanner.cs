namespace ContosoDashboard.Services;

public sealed class LocalFileScanner : IFileScanner
{
    public async Task<bool> IsSafeAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (!content.CanRead) return false;
        if (content.CanSeek) content.Position = 0;
        var buffer = new byte[4096];
        var read = await content.ReadAsync(buffer.AsMemory(), cancellationToken);
        if (read == 0) return false;
        if (content.CanSeek) content.Position = 0;
        var sample = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
        return !sample.Contains("EICAR", StringComparison.OrdinalIgnoreCase);
    }
}
