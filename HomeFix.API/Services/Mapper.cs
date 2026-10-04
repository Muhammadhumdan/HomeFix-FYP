using HomeFix.API.DTOs;
using HomeFix.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeFix.API.Services;

/// <summary>Entity -> DTO mapping helpers shared by the services.</summary>
public static class Mapper
{
    public static UserDto ToDto(User u) => new()
    {
        Id = u.Id, FullName = u.FullName, Email = u.Email, Role = u.Role.ToString(),
        Phone = u.Phone, City = u.City, Address = u.Address,
        ProviderStatus = u.ProviderProfile?.ApprovalStatus.ToString(),
        ProviderId = u.ProviderProfile?.Id
    };

    public static double Average(IEnumerable<Review> reviews)
    {
        var list = reviews.ToList();
        return list.Count == 0 ? 0 : Math.Round(list.Average(r => r.Rating), 1);
    }

    public static ServiceDto ToDto(Service s, ProviderProfile p, double rating) => new()
    {
        Id = s.Id, ProviderId = p.Id, ProviderName = p.BusinessName, ServiceArea = p.ServiceArea,
        CategoryId = s.CategoryId, CategoryName = s.Category?.Name ?? p.Category?.Name ?? "",
        Title = s.Title, Description = s.Description, Price = s.Price, PriceUnit = s.PriceUnit,
        IsActive = s.IsActive, ProviderRating = rating
    };

    public static ReviewDto ToDto(Review r) => new()
    {
        Id = r.Id, BookingId = r.BookingId, ProviderId = r.ProviderProfileId,
        ProviderName = r.ProviderProfile?.BusinessName ?? "", CustomerId = r.CustomerId,
        CustomerName = r.Customer?.FullName ?? "", ServiceTitle = r.Booking?.Service?.Title ?? "",
        Rating = r.Rating, Comment = r.Comment, CreatedAt = r.CreatedAt
    };

    public static ProviderDto ToDto(ProviderProfile p, bool withReviews = false)
    {
        var rating = Average(p.Reviews);
        var dto = new ProviderDto
        {
            Id = p.Id, UserId = p.UserId, ProviderName = p.User.FullName, BusinessName = p.BusinessName,
            Bio = p.Bio, CategoryId = p.CategoryId, CategoryName = p.Category.Name, CategoryIcon = p.Category.Icon,
            ServiceArea = p.ServiceArea, HourlyRate = p.HourlyRate, ExperienceYears = p.ExperienceYears,
            IsAvailable = p.IsAvailable, AvailableDays = p.AvailableDays, StartTime = p.StartTime, EndTime = p.EndTime,
            ApprovalStatus = p.ApprovalStatus.ToString(), RejectionReason = p.RejectionReason,
            AverageRating = rating, ReviewCount = p.Reviews.Count,
            Services = p.Services.Where(s => s.IsActive).OrderBy(s => s.Price)
                .Select(s => ToDto(s, p, rating)).ToList()
        };
        if (withReviews)
            dto.Reviews = p.Reviews.OrderByDescending(r => r.CreatedAt).Select(r => { r.ProviderProfile = p; return ToDto(r); }).ToList();
        return dto;
    }

    public static BookingDto ToDto(Booking b, int viewerId = 0)
    {
        var shared = b.Status == BookingStatus.Accepted || b.Status == BookingStatus.Completed; // phone numbers shared only after acceptance
        var dto = new BookingDto
        {
            Id = b.Id, Status = b.Status.ToString(), ScheduledDate = b.ScheduledDate, Address = b.Address, Notes = b.Notes,
            EstimatedPrice = b.EstimatedPrice, CreatedAt = b.CreatedAt, UpdatedAt = b.UpdatedAt,
            ServiceId = b.ServiceId, ServiceTitle = b.Service.Title, PriceUnit = b.Service.PriceUnit,
            CategoryName = b.Service.Category?.Name ?? "",
            ProviderId = b.ProviderProfileId, ProviderName = b.ProviderProfile.BusinessName,
            ProviderPhone = shared ? b.ProviderProfile.User?.Phone : null,
            CustomerId = b.CustomerId, CustomerName = b.Customer.FullName,
            CustomerPhone = shared ? b.Customer.Phone : null,
            CanReview = b.Status == BookingStatus.Completed && b.Review == null && b.CustomerId == viewerId
        };
        if (b.Review != null)
            dto.Review = new ReviewDto
            {
                Id = b.Review.Id, BookingId = b.Id, ProviderId = b.ProviderProfileId, ProviderName = dto.ProviderName,
                CustomerId = b.CustomerId, CustomerName = dto.CustomerName, ServiceTitle = dto.ServiceTitle,
                Rating = b.Review.Rating, Comment = b.Review.Comment, CreatedAt = b.Review.CreatedAt
            };
        return dto;
    }
}

public static class QueryExtensions
{
    /// <summary>Loads everything needed to build a ProviderDto.</summary>
    public static IQueryable<ProviderProfile> WithGraph(this IQueryable<ProviderProfile> q, bool detailed = false)
    {
        IQueryable<ProviderProfile> r = q.AsNoTracking()
            .Include(p => p.User).Include(p => p.Category).Include(p => p.Services).Include(p => p.Reviews);
        if (detailed)
        {
            r = r.Include(p => p.Reviews).ThenInclude(x => x.Customer);
            r = r.Include(p => p.Reviews).ThenInclude(x => x.Booking).ThenInclude(b => b.Service);
        }
        return r.AsSplitQuery();
    }

    public static IQueryable<Booking> WithGraph(this IQueryable<Booking> q) => q
        .Include(b => b.Customer)
        .Include(b => b.ProviderProfile).ThenInclude(p => p.User)
        .Include(b => b.Service).ThenInclude(s => s.Category)
        .Include(b => b.Review);
}
