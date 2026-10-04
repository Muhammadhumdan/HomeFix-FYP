using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Middleware;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

public class AdminService(HomeFixDbContext db) : IAdminService
{
    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var byStatus = await db.Bookings.GroupBy(b => b.Status).Select(g => new { g.Key, Count = g.Count() }).ToListAsync();
        var stats = new DashboardStatsDto
        {
            TotalCustomers = await db.Users.CountAsync(u => u.Role == UserRole.Customer),
            TotalProviders = await db.ProviderProfiles.CountAsync(),
            PendingApprovals = await db.ProviderProfiles.CountAsync(p => p.ApprovalStatus == ApprovalStatus.Pending),
            ApprovedProviders = await db.ProviderProfiles.CountAsync(p => p.ApprovalStatus == ApprovalStatus.Approved),
            TotalServices = await db.Services.CountAsync(s => s.IsActive),
            TotalCategories = await db.ServiceCategories.CountAsync(),
            TotalBookings = await db.Bookings.CountAsync(),
            TotalReviews = await db.Reviews.CountAsync(),
            OpenReports = await db.Reports.CountAsync(r => r.Status == ReportStatus.Open)
        };
        foreach (var s in Enum.GetValues<BookingStatus>())
            stats.BookingsByStatus[s.ToString()] = byStatus.FirstOrDefault(x => x.Key == s)?.Count ?? 0;
        stats.PendingBookings = stats.BookingsByStatus["Pending"];
        stats.CompletedBookings = stats.BookingsByStatus["Completed"];
        return stats;
    }

    public async Task<List<AdminUserDto>> GetUsersAsync(string? role)
    {
        var q = db.Users.AsNoTracking().Include(u => u.ProviderProfile).AsQueryable();
        if (!string.IsNullOrWhiteSpace(role) && Enum.TryParse<UserRole>(role, true, out var r)) q = q.Where(u => u.Role == r);
        var list = await q.OrderByDescending(u => u.CreatedAt).ToListAsync();
        return list.Select(ToAdminUser).ToList();
    }

    public async Task<List<ProviderDto>> GetProvidersAsync(string? status)
    {
        var q = db.ProviderProfiles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ApprovalStatus>(status, true, out var st))
            q = q.Where(p => p.ApprovalStatus == st);
        var list = await q.WithGraph().ToListAsync();
        return list.Select(p => Mapper.ToDto(p)).OrderBy(p => p.ApprovalStatus == "Pending" ? 0 : 1).ThenBy(p => p.BusinessName).ToList();
    }

    public async Task<ProviderDto> ApproveProviderAsync(int providerId)
    {
        var p = await db.ProviderProfiles.FindAsync(providerId) ?? throw new AppException(404, "Provider not found.");
        p.ApprovalStatus = ApprovalStatus.Approved; p.RejectionReason = null;
        await db.SaveChangesAsync();
        return await Load(providerId);
    }

    public async Task<ProviderDto> RejectProviderAsync(int providerId, string? reason)
    {
        var p = await db.ProviderProfiles.FindAsync(providerId) ?? throw new AppException(404, "Provider not found.");
        p.ApprovalStatus = ApprovalStatus.Rejected;
        p.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "Application did not meet verification requirements." : reason.Trim();
        await db.SaveChangesAsync();
        return await Load(providerId);
    }

    public async Task<AdminUserDto> ToggleUserActiveAsync(int adminId, int userId)
    {
        var u = await db.Users.Include(x => x.ProviderProfile).FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new AppException(404, "User not found.");
        if (u.Id == adminId || u.Role == UserRole.Admin) throw new AppException(400, "Admin accounts cannot be deactivated.");
        u.IsActive = !u.IsActive;
        await db.SaveChangesAsync();
        return ToAdminUser(u);
    }

    public async Task<List<BookingDto>> GetBookingsAsync(string? status)
    {
        var q = db.Bookings.AsNoTracking().WithGraph();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var st)) q = q.Where(b => b.Status == st);
        var list = await q.OrderByDescending(b => b.CreatedAt).ToListAsync();
        return list.Select(b => Mapper.ToDto(b)).ToList();
    }

    public async Task<List<ActivityDto>> GetActivityAsync()
    {
        var items = new List<ActivityDto>();
        var users = await db.Users.AsNoTracking().OrderByDescending(u => u.CreatedAt).Take(10).ToListAsync();
        items.AddRange(users.Select(u => new ActivityDto { Type = "User", At = u.CreatedAt, Message = $"{u.FullName} registered as {u.Role}" }));

        var bookings = await db.Bookings.AsNoTracking().Include(b => b.Customer).Include(b => b.ProviderProfile).Include(b => b.Service)
            .OrderByDescending(b => b.UpdatedAt).Take(15).ToListAsync();
        items.AddRange(bookings.Select(b => new ActivityDto
        {
            Type = "Booking", At = b.UpdatedAt,
            Message = $"{b.Customer.FullName} - {b.Service.Title} with {b.ProviderProfile.BusinessName}: {b.Status}"
        }));

        var reviews = await db.Reviews.AsNoTracking().Include(r => r.Customer).Include(r => r.ProviderProfile)
            .OrderByDescending(r => r.CreatedAt).Take(10).ToListAsync();
        items.AddRange(reviews.Select(r => new ActivityDto
        {
            Type = "Review", At = r.CreatedAt, Message = $"{r.Customer.FullName} rated {r.ProviderProfile.BusinessName} {r.Rating}/5"
        }));

        var reports = await db.Reports.AsNoTracking().Include(r => r.Reporter).Include(r => r.ReportedUser)
            .OrderByDescending(r => r.CreatedAt).Take(10).ToListAsync();
        items.AddRange(reports.Select(r => new ActivityDto
        {
            Type = "Report", At = r.CreatedAt, Message = $"{r.Reporter.FullName} reported {r.ReportedUser.FullName}: {r.Reason}"
        }));

        return items.OrderByDescending(i => i.At).Take(30).ToList();
    }

    public async Task<List<ReportDto>> GetReportsAsync(string? status)
    {
        var q = ReportQuery();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ReportStatus>(status, true, out var st)) q = q.Where(r => r.Status == st);
        var list = await q.ToListAsync();
        return list.OrderBy(r => r.Status == ReportStatus.Open ? 0 : 1).ThenByDescending(r => r.CreatedAt).Select(ToReportDto).ToList();
    }

    public async Task<ReportDto> CreateReportAsync(int reporterId, CreateReportRequest r)
    {
        if (r.ReportedUserId == reporterId) throw new AppException(400, "You cannot report yourself.");
        if (!await db.Users.AnyAsync(u => u.Id == r.ReportedUserId)) throw new AppException(404, "The reported user does not exist.");
        if (r.ReviewId.HasValue && !await db.Reviews.AnyAsync(x => x.Id == r.ReviewId)) throw new AppException(404, "The reported review does not exist.");
        if (await db.Reports.AnyAsync(x => x.ReporterId == reporterId && x.ReportedUserId == r.ReportedUserId && x.Status == ReportStatus.Open))
            throw new AppException(409, "You have already reported this user. An admin will review it.");

        var report = new Report
        {
            ReporterId = reporterId, ReportedUserId = r.ReportedUserId, ReviewId = r.ReviewId,
            Reason = r.Reason.Trim(), Details = r.Details?.Trim()
        };
        db.Reports.Add(report);
        await db.SaveChangesAsync();
        return ToReportDto(await ReportQuery().FirstAsync(x => x.Id == report.Id));
    }

    public async Task<ReportDto> SetReportStatusAsync(int id, ReportStatus status)
    {
        var report = await db.Reports.FindAsync(id) ?? throw new AppException(404, "Report not found.");
        report.Status = status;
        await db.SaveChangesAsync();
        return ToReportDto(await ReportQuery().FirstAsync(x => x.Id == id));
    }

    private IQueryable<Report> ReportQuery() =>
        db.Reports.AsNoTracking().Include(r => r.Reporter).Include(r => r.ReportedUser).Include(r => r.Review);

    private async Task<ProviderDto> Load(int id) =>
        Mapper.ToDto(await db.ProviderProfiles.Where(p => p.Id == id).WithGraph().FirstAsync());

    private static AdminUserDto ToAdminUser(User u) => new()
    {
        Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role.ToString(), Phone = u.Phone, City = u.City,
        IsActive = u.IsActive, CreatedAt = u.CreatedAt, ProviderStatus = u.ProviderProfile?.ApprovalStatus.ToString()
    };

    private static ReportDto ToReportDto(Report r) => new()
    {
        Id = r.Id, ReporterId = r.ReporterId, ReporterName = r.Reporter.FullName, ReportedUserId = r.ReportedUserId,
        ReportedUserName = r.ReportedUser.FullName, ReportedUserActive = r.ReportedUser.IsActive,
        ReviewId = r.ReviewId, ReviewComment = r.Review?.Comment, Reason = r.Reason, Details = r.Details,
        Status = r.Status.ToString(), CreatedAt = r.CreatedAt
    };
}
