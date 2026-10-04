using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Middleware;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

public class ReviewService(HomeFixDbContext db) : IReviewService
{
    public async Task<ReviewDto> CreateAsync(int customerId, CreateReviewRequest r)
    {
        var booking = await db.Bookings.Include(b => b.Review).FirstOrDefaultAsync(b => b.Id == r.BookingId)
                      ?? throw new AppException(404, "Booking not found.");
        if (booking.CustomerId != customerId)
            throw new AppException(403, "You can only review your own bookings.");
        if (booking.Status != BookingStatus.Completed)
            throw new AppException(409, "You can only review a booking after it is completed.");
        if (booking.Review != null)
            throw new AppException(409, "You have already reviewed this booking.");

        var review = new Review
        {
            BookingId = booking.Id, CustomerId = customerId, ProviderProfileId = booking.ProviderProfileId,
            Rating = r.Rating, Comment = r.Comment.Trim()
        };
        db.Reviews.Add(review);
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException) { throw new AppException(409, "You have already reviewed this booking."); } // unique index safety net

        var saved = await db.Reviews.AsNoTracking().Include(x => x.Customer).Include(x => x.ProviderProfile)
            .Include(x => x.Booking).ThenInclude(x => x.Service).FirstAsync(x => x.Id == review.Id);
        return Mapper.ToDto(saved);
    }

    public async Task<List<ReviewDto>> GetForProviderAsync(int providerId)
    {
        var list = await Base().Where(r => r.ProviderProfileId == providerId && r.ProviderProfile.ApprovalStatus == ApprovalStatus.Approved)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        return list.Select(r => Mapper.ToDto(r)).ToList();
    }

    public async Task<List<ReviewDto>> GetMineAsync(int userId, UserRole role)
    {
        var q = Base();
        q = role == UserRole.Provider ? q.Where(r => r.ProviderProfile.UserId == userId) : q.Where(r => r.CustomerId == userId);
        var list = await q.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return list.Select(r => Mapper.ToDto(r)).ToList();
    }

    private IQueryable<Review> Base() => db.Reviews.AsNoTracking().Include(r => r.Customer)
        .Include(r => r.ProviderProfile).Include(r => r.Booking).ThenInclude(b => b.Service);
}
