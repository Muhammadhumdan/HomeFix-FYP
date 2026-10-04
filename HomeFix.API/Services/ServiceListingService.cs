using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Middleware;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

public class ServiceListingService(HomeFixDbContext db) : IServiceListingService
{
    private async Task<Dictionary<int, double>> RatingsAsync() =>
        await db.Reviews.GroupBy(r => r.ProviderProfileId)
            .Select(g => new { Id = g.Key, Avg = g.Average(r => (double)r.Rating) })
            .ToDictionaryAsync(x => x.Id, x => Math.Round(x.Avg, 1));

    public async Task<List<ServiceDto>> GetPublicAsync(int? categoryId, string? q)
    {
        var query = db.Services.AsNoTracking().Include(s => s.Category).Include(s => s.ProviderProfile)
            .Where(s => s.IsActive && s.ProviderProfile.ApprovalStatus == ApprovalStatus.Approved && s.ProviderProfile.User.IsActive);
        if (categoryId.HasValue) query = query.Where(s => s.CategoryId == categoryId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var t = q.Trim();
            query = query.Where(s => s.Title.Contains(t) || (s.Description != null && s.Description.Contains(t)) ||
                                     s.Category.Name.Contains(t) || s.ProviderProfile.BusinessName.Contains(t));
        }
        var list = await query.OrderBy(s => s.Price).ToListAsync();
        var ratings = await RatingsAsync();
        return list.Select(s => Mapper.ToDto(s, s.ProviderProfile, ratings.GetValueOrDefault(s.ProviderProfileId))).ToList();
    }

    public async Task<List<ServiceDto>> GetMineAsync(int userId)
    {
        var p = await Profile(userId);
        var list = await db.Services.AsNoTracking().Include(s => s.Category)
            .Where(s => s.ProviderProfileId == p.Id).OrderByDescending(s => s.CreatedAt).ToListAsync();
        return list.Select(s => Mapper.ToDto(s, p, 0)).ToList();
    }

    public async Task<ServiceDto> GetMineByIdAsync(int userId, int id)
    {
        var p = await Profile(userId);
        var s = await db.Services.AsNoTracking().Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.Id == id && x.ProviderProfileId == p.Id)
                ?? throw new AppException(404, "Service not found.");
        return Mapper.ToDto(s, p, 0);
    }

    public async Task<ServiceDto> CreateAsync(int userId, ServiceRequest r)
    {
        var p = await Profile(userId);
        var categoryId = await ResolveCategory(r.CategoryId ?? p.CategoryId);
        var s = new Service
        {
            ProviderProfileId = p.Id, CategoryId = categoryId, Title = r.Title.Trim(),
            Description = r.Description?.Trim(), Price = r.Price, PriceUnit = r.PriceUnit, IsActive = r.IsActive
        };
        db.Services.Add(s);
        await db.SaveChangesAsync();
        return await GetMineByIdAsync(userId, s.Id);
    }

    public async Task<ServiceDto> UpdateAsync(int userId, int id, ServiceRequest r)
    {
        var p = await Profile(userId);
        var s = await db.Services.FirstOrDefaultAsync(x => x.Id == id && x.ProviderProfileId == p.Id)
                ?? throw new AppException(404, "Service not found.");
        s.CategoryId = await ResolveCategory(r.CategoryId ?? s.CategoryId);
        s.Title = r.Title.Trim(); s.Description = r.Description?.Trim();
        s.Price = r.Price; s.PriceUnit = r.PriceUnit; s.IsActive = r.IsActive;
        await db.SaveChangesAsync();
        return await GetMineByIdAsync(userId, id);
    }

    public async Task DeleteAsync(int userId, int id)
    {
        var p = await Profile(userId);
        var s = await db.Services.FirstOrDefaultAsync(x => x.Id == id && x.ProviderProfileId == p.Id)
                ?? throw new AppException(404, "Service not found.");
        // Keep booking history intact: services that have bookings are hidden instead of deleted
        if (await db.Bookings.AnyAsync(b => b.ServiceId == id)) s.IsActive = false;
        else db.Services.Remove(s);
        await db.SaveChangesAsync();
    }

    private async Task<ProviderProfile> Profile(int userId) =>
        await db.ProviderProfiles.AsNoTracking().Include(p => p.Category).FirstOrDefaultAsync(p => p.UserId == userId)
        ?? throw new AppException(404, "Provider profile not found.");

    private async Task<int> ResolveCategory(int id)
    {
        if (!await db.ServiceCategories.AnyAsync(c => c.Id == id && c.IsActive))
            throw new AppException(400, "The selected service category does not exist.");
        return id;
    }
}
