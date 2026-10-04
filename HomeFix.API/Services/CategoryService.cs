using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Middleware;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

public class CategoryService(HomeFixDbContext db) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(bool includeInactive)
    {
        var q = db.ServiceCategories.AsNoTracking().AsQueryable();
        if (!includeInactive) q = q.Where(c => c.IsActive);
        return await q.OrderBy(c => c.Name).Select(c => new CategoryDto
        {
            Id = c.Id, Name = c.Name, Description = c.Description, Icon = c.Icon, IsActive = c.IsActive,
            ProviderCount = c.Providers.Count(p => p.ApprovalStatus == ApprovalStatus.Approved),
            ServiceCount = c.Services.Count(s => s.IsActive)
        }).ToListAsync();
    }

    public async Task<CategoryDto> CreateAsync(CategoryRequest r)
    {
        var name = r.Name.Trim();
        if (await db.ServiceCategories.AnyAsync(c => c.Name == name))
            throw new AppException(409, "A category with this name already exists.");
        var c = new ServiceCategory { Name = name, Description = r.Description?.Trim(), Icon = r.Icon, IsActive = r.IsActive };
        db.ServiceCategories.Add(c);
        await db.SaveChangesAsync();
        return ToDto(c);
    }

    public async Task<CategoryDto> UpdateAsync(int id, CategoryRequest r)
    {
        var c = await db.ServiceCategories.FindAsync(id) ?? throw new AppException(404, "Category not found.");
        var name = r.Name.Trim();
        if (await db.ServiceCategories.AnyAsync(x => x.Name == name && x.Id != id))
            throw new AppException(409, "A category with this name already exists.");
        c.Name = name; c.Description = r.Description?.Trim(); c.Icon = r.Icon; c.IsActive = r.IsActive;
        await db.SaveChangesAsync();
        return ToDto(c);
    }

    public async Task DeleteAsync(int id)
    {
        var c = await db.ServiceCategories.FindAsync(id) ?? throw new AppException(404, "Category not found.");
        var inUse = await db.ProviderProfiles.AnyAsync(p => p.CategoryId == id) || await db.Services.AnyAsync(s => s.CategoryId == id);
        if (inUse)
            throw new AppException(409, "This category is used by providers or services. Deactivate it instead of deleting.");
        db.ServiceCategories.Remove(c);
        await db.SaveChangesAsync();
    }

    private static CategoryDto ToDto(ServiceCategory c) => new()
    {
        Id = c.Id, Name = c.Name, Description = c.Description, Icon = c.Icon, IsActive = c.IsActive
    };
}
