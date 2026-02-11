namespace RailTix.Services.Cms
{
    public interface ICmsUrlService
    {
        string NormalizeSegment(string input);
        string NormalizePath(string path);
        string NormalizeCustomUrl(string? path);
        string EnsureLeadingSlash(string path);
        string? GetTopLevelSegment(string path);
    }
}

