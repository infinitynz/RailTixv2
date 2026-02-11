using System.Collections.Generic;
using System.Threading.Tasks;

namespace RailTix.Services.Cms
{
    public interface ICmsReservedRouteService
    {
        Task<HashSet<string>> GetReservedSegmentsAsync();
        Task<bool> IsReservedSegmentAsync(string? segment);
        void InvalidateCache();
    }
}

