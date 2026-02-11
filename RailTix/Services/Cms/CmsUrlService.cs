using System;
using System.Linq;
using System.Text;

namespace RailTix.Services.Cms
{
    public class CmsUrlService : ICmsUrlService
    {
        public string NormalizeSegment(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var trimmed = input.Trim().ToLowerInvariant();
            var builder = new StringBuilder(trimmed.Length);
            bool lastWasHyphen = false;

            foreach (var ch in trimmed)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    builder.Append(ch);
                    lastWasHyphen = false;
                    continue;
                }

                if (!lastWasHyphen)
                {
                    builder.Append('-');
                    lastWasHyphen = true;
                }
            }

            var normalized = builder.ToString().Trim('-');
            return normalized;
        }

        public string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                return "/";
            }

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var normalizedSegments = segments
                .Select(NormalizeSegment)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();

            return "/" + string.Join("/", normalizedSegments);
        }

        public string NormalizeCustomUrl(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            return NormalizePath(path);
        }

        public string EnsureLeadingSlash(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return "/";
            }

            return path.StartsWith("/", StringComparison.Ordinal) ? path : "/" + path;
        }

        public string? GetTopLevelSegment(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                return null;
            }

            var trimmed = path.Trim('/');
            var segment = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return segment;
        }
    }
}

