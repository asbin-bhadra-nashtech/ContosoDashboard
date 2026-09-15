namespace ContosoDashboard.Services;

public static class DocumentValidation
{
    public const long MaxFileSizeBytes = 25 * 1024 * 1024;

    public static readonly IReadOnlyList<string> Categories = new[]
    {
        "Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"
    };

    public static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".txt"] = "text/plain",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

    public static bool IsAllowed(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName);
        return AllowedContentTypes.TryGetValue(extension, out var expectedType)
            && (string.Equals(expectedType, contentType, StringComparison.OrdinalIgnoreCase)
                || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase));
    }

    public static string GetSafeFileName(string fileName)
    {
        return Path.GetFileName(fileName).Replace("\0", string.Empty);
    }
}
