using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

public class SearchService(HomeFixDbContext db) : ISearchService
{
    public async Task<List<ProviderDto>> SearchProvidersAsync(SearchQuery s)
    {
        // Unapproved providers never appear in public search
        var q = db.ProviderProfiles.Where(p => p.ApprovalStatus == ApprovalStatus.Approved && p.User.IsActive);

        if (s.CategoryId.HasValue) q = q.Where(p => p.CategoryId == s.CategoryId);
        if (!string.IsNullOrWhiteSpace(s.Location))
        {
            var loc = s.Location.Trim();
            q = q.Where(p => p.ServiceArea.Contains(loc) || (p.User.City != null && p.User.City.Contains(loc)));
        }
        if (!string.IsNullOrWhiteSpace(s.Q))
        {
            var t = s.Q.Trim();
            q = q.Where(p => p.BusinessName.Contains(t) || p.User.FullName.Contains(t) || p.Category.Name.Contains(t) ||
                             (p.Bio != null && p.Bio.Contains(t)) || p.Services.Any(x => x.IsActive && x.Title.Contains(t)));
        }
        if (s.MinRate.HasValue)
        {
            var min = s.MinRate.Value;
            q = q.Where(p => p.HourlyRate >= min);
        }
        if (s.MaxRate.HasValue)
        {
            var max = s.MaxRate.Value;
            q = q.Where(p => p.HourlyRate <= max || p.Services.Any(x => x.IsActive && x.Price <= max));
        }
        if (s.Date.HasValue)
        {
            var day = s.Date.Value.DayOfWeek.ToString()[..3];
            q = q.Where(p => p.IsAvailable && p.AvailableDays.Contains(day));
        }
        else if (s.AvailableOnly)
        {
            q = q.Where(p => p.IsAvailable);
        }

        var list = await q.WithGraph().ToListAsync();
        var dtos = list.Select(p => Mapper.ToDto(p)).ToList();
        if (s.MinRating.HasValue) dtos = dtos.Where(d => d.AverageRating >= s.MinRating.Value).ToList();
        return dtos.OrderByDescending(d => d.AverageRating).ThenBy(d => d.HourlyRate).ToList();
    }
}
