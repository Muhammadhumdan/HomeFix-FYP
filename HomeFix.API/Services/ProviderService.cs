using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Middleware;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

public class ProviderService(HomeFixDbContext db) : IProviderService
{
    private static readonly string[] Days = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    public async Task<ProviderDto> GetPublicAsync(int id)
    {
        // Only approved providers are publicly visible
        var p = await db.ProviderProfiles
            .Where(x => x.Id == id && x.ApprovalStatus == ApprovalStatus.Approved && x.User.IsActive)
            .WithGraph(true).FirstOrDefaultAsync()
            ?? throw new AppException(404, "Provider not found.");
        return Mapper.ToDto(p, true);
    }

    public async Task<ProviderDto> GetMineAsync(int userId)
    {
        var p = await db.ProviderProfiles.Where(x => x.UserId == userId).WithGraph(true).FirstOrDefaultAsync()
                ?? throw new AppException(404, "Provider profile not found.");
        return Mapper.ToDto(p, true);
    }

    public async Task<ProviderDto> UpdateMineAsync(int userId, UpdateProviderProfileRequest r)
    {
        var p = await db.ProviderProfiles.FirstOrDefaultAsync(x => x.UserId == userId)
                ?? throw new AppException(404, "Provider profile not found.");
        if (!await db.ServiceCategories.AnyAsync(c => c.Id == r.CategoryId && c.IsActive))
            throw new AppException(400, "The selected service category does not exist.");
        p.BusinessName = r.BusinessName.Trim(); p.Bio = r.Bio?.Trim(); p.CategoryId = r.CategoryId;
        p.ServiceArea = r.ServiceArea.Trim(); p.HourlyRate = r.HourlyRate; p.ExperienceYears = r.ExperienceYears;
        if (p.ApprovalStatus == ApprovalStatus.Rejected) { p.ApprovalStatus = ApprovalStatus.Pending; p.RejectionReason = null; } // resubmit
        await db.SaveChangesAsync();
        return await GetMineAsync(userId);
    }

    public async Task<ProviderDto> UpdateAvailabilityAsync(int userId, AvailabilityRequest r)
    {
        var p = await db.ProviderProfiles.FirstOrDefaultAsync(x => x.UserId == userId)
                ?? throw new AppException(404, "Provider profile not found.");
        var chosen = r.AvailableDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(d => Days.FirstOrDefault(x => x.Equals(d, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (chosen.Count == 0 || chosen.Any(d => d is null))
            throw new AppException(400, "Choose at least one valid working day (Mon-Sun).");
        if (string.CompareOrdinal(r.StartTime, r.EndTime) >= 0)
            throw new AppException(400, "End time must be after start time.");
        p.IsAvailable = r.IsAvailable;
        p.AvailableDays = string.Join(",", Days.Where(d => chosen.Contains(d)));
        p.StartTime = r.StartTime; p.EndTime = r.EndTime;
        await db.SaveChangesAsync();
        return await GetMineAsync(userId);
    }

    public async Task<List<ProviderDto>> GetFeaturedAsync(int count)
    {
        var list = await db.ProviderProfiles
            .Where(x => x.ApprovalStatus == ApprovalStatus.Approved && x.User.IsActive)
            .WithGraph().ToListAsync();
        return list.Select(p => Mapper.ToDto(p))
            .OrderByDescending(p => p.AverageRating).ThenByDescending(p => p.ReviewCount)
            .Take(count).ToList();
    }
}
