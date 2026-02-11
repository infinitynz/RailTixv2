using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RailTix.Data;

namespace RailTix.Services.Cms
{
    public class CmsReservedRouteService : ICmsReservedRouteService
    {
        private const string CacheKey = "cms_reserved_segments";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(30);

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ICmsUrlService _urlService;

        public CmsReservedRouteService(ApplicationDbContext db, IMemoryCache cache, ICmsUrlService urlService)
        {
            _db = db;
            _cache = cache;
            _urlService = urlService;
        }

        public async Task<HashSet<string>> GetReservedSegmentsAsync()
        {
            if (_cache.TryGetValue(CacheKey, out HashSet<string>? cached) && cached != null)
            {
                return cached;
            }

            var segments = await _db.CmsReservedRoutes
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Select(r => r.Segment)
                .ToListAsync();

            var normalized = segments
                .Select(s => _urlService.NormalizeSegment(s))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _cache.Set(CacheKey, normalized, CacheTtl);
            return normalized;
        }

        public async Task<bool> IsReservedSegmentAsync(string? segment)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                return false;
            }

            var normalized = _urlService.NormalizeSegment(segment);
            var reserved = await GetReservedSegmentsAsync();
            return reserved.Contains(normalized);
        }

        public void InvalidateCache()
        {
            _cache.Remove(CacheKey);
        }
    }
}

