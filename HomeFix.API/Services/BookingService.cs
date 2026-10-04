using HomeFix.API.Data;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Middleware;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

public class BookingService(HomeFixDbContext db) : IBookingService
{
    // Allowed status transitions. Anything else (e.g. Rejected -> Completed) is refused.
    private static readonly Dictionary<BookingStatus, BookingStatus[]> Allowed = new()
    {
        [BookingStatus.Pending] = new[] { BookingStatus.Accepted, BookingStatus.Rejected, BookingStatus.Cancelled },
        [BookingStatus.Accepted] = new[] { BookingStatus.Completed, BookingStatus.Cancelled },
        [BookingStatus.Rejected] = Array.Empty<BookingStatus>(),
        [BookingStatus.Completed] = Array.Empty<BookingStatus>(),
        [BookingStatus.Cancelled] = Array.Empty<BookingStatus>()
    };

    public async Task<BookingDto> CreateAsync(int customerId, CreateBookingRequest r)
    {
        var service = await db.Services.Include(s => s.ProviderProfile).ThenInclude(p => p.User)
                          .FirstOrDefaultAsync(s => s.Id == r.ServiceId)
                      ?? throw new AppException(404, "Service not found.");
        var provider = service.ProviderProfile;

        if (!service.IsActive || provider.ApprovalStatus != ApprovalStatus.Approved || !provider.User.IsActive)
            throw new AppException(400, "This service is not available for booking.");
        if (!provider.IsAvailable)
            throw new AppException(400, "This provider is currently not accepting bookings.");
        if (r.ScheduledDate.Date < DateTime.Today)
            throw new AppException(400, "Please choose today or a future date.");
        var day = r.ScheduledDate.DayOfWeek.ToString()[..3];
        if (!provider.AvailableDays.Split(',').Contains(day))
            throw new AppException(400, $"This provider does not work on {r.ScheduledDate.DayOfWeek}. Available days: {provider.AvailableDays.Replace(",", ", ")}.");

        var booking = new Booking
        {
            CustomerId = customerId, ProviderProfileId = provider.Id, ServiceId = service.Id,
            ScheduledDate = r.ScheduledDate.Date, Address = r.Address.Trim(), Notes = r.Notes?.Trim(),
            EstimatedPrice = service.Price, Status = BookingStatus.Pending
        };
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();
        return await GetByIdAsync(customerId, UserRole.Customer, booking.Id);
    }

    public async Task<List<BookingDto>> GetMineAsync(int userId, UserRole role, string? status)
    {
        var q = db.Bookings.AsNoTracking().WithGraph();
        q = role switch
        {
            UserRole.Customer => q.Where(b => b.CustomerId == userId),
            UserRole.Provider => q.Where(b => b.ProviderProfile.UserId == userId),
            _ => q
        };
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<BookingStatus>(status, true, out var st))
            q = q.Where(b => b.Status == st);
        var list = await q.OrderByDescending(b => b.CreatedAt).ToListAsync();
        return list.Select(b => Mapper.ToDto(b, userId)).ToList();
    }

    public async Task<BookingDto> GetByIdAsync(int userId, UserRole role, int id)
    {
        var b = await db.Bookings.AsNoTracking().WithGraph().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Booking not found.");
        var allowed = role == UserRole.Admin || b.CustomerId == userId || b.ProviderProfile.UserId == userId;
        if (!allowed) throw new AppException(404, "Booking not found."); // do not reveal other people's bookings
        return Mapper.ToDto(b, userId);
    }

    public async Task<BookingDto> ChangeStatusAsync(int userId, UserRole role, int id, BookingStatus target)
    {
        var b = await db.Bookings.WithGraph().FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AppException(404, "Booking not found.");
        var isCustomer = b.CustomerId == userId;
        var isProvider = b.ProviderProfile.UserId == userId;
        if (!isCustomer && !isProvider) throw new AppException(404, "Booking not found.");

        // Who may perform which action
        if (target == BookingStatus.Cancelled && !isCustomer)
            throw new AppException(403, "Only the customer can cancel a booking.");
        if (target is BookingStatus.Accepted or BookingStatus.Rejected or BookingStatus.Completed && !isProvider)
            throw new AppException(403, "Only the provider can accept, reject or complete a booking.");
        if (target == BookingStatus.Pending)
            throw new AppException(400, "A booking cannot be moved back to Pending.");

        if (!Allowed[b.Status].Contains(target))
            throw new AppException(409, $"A {b.Status} booking cannot be changed to {target}.");

        b.Status = target;
        b.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Mapper.ToDto(b, userId);
    }
}
