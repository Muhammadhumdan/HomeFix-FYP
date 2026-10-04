using System.ComponentModel.DataAnnotations;

namespace HomeFix.API.DTOs;

// ---------- Auth / Users ----------
public class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string FullName { get; set; } = "";
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = "";
    [Required, StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Customer";   // Customer | Provider
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(100)] public string? City { get; set; }
    // Provider-only fields
    [StringLength(150)] public string? BusinessName { get; set; }
    public int? CategoryId { get; set; }
    [StringLength(200)] public string? ServiceArea { get; set; }
    [Range(0, 1000000)] public decimal? HourlyRate { get; set; }
    [Range(0, 60)] public int? ExperienceYears { get; set; }
    [StringLength(1000)] public string? Bio { get; set; }
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}

public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? ProviderStatus { get; set; }
    public int? ProviderId { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100, MinimumLength = 2)] public string FullName { get; set; } = "";
    [StringLength(30)] public string? Phone { get; set; }
    [StringLength(100)] public string? City { get; set; }
    [StringLength(300)] public string? Address { get; set; }
}

// ---------- Categories / Services / Providers ----------
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public bool IsActive { get; set; }
    public int ProviderCount { get; set; }
    public int ServiceCount { get; set; }
}

public class CategoryRequest
{
    [Required, StringLength(80, MinimumLength = 2)] public string Name { get; set; } = "";
    [StringLength(300)] public string? Description { get; set; }
    [StringLength(10)] public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ServiceDto
{
    public int Id { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = "";
    public string ServiceArea { get; set; } = "";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string PriceUnit { get; set; } = "hour";
    public bool IsActive { get; set; }
    public double ProviderRating { get; set; }
}

public class ServiceRequest
{
    [Required, StringLength(150, MinimumLength = 3)] public string Title { get; set; } = "";
    [StringLength(1000)] public string? Description { get; set; }
    [Range(1, 10000000, ErrorMessage = "Price must be greater than 0.")] public decimal Price { get; set; }
    [Required, RegularExpression("^(hour|day|job)$", ErrorMessage = "Price unit must be hour, day or job.")] public string PriceUnit { get; set; } = "hour";
    public int? CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProviderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ProviderName { get; set; } = "";
    public string BusinessName { get; set; } = "";
    public string? Bio { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string? CategoryIcon { get; set; }
    public string ServiceArea { get; set; } = "";
    public decimal HourlyRate { get; set; }
    public int ExperienceYears { get; set; }
    public bool IsAvailable { get; set; }
    public string AvailableDays { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public string ApprovalStatus { get; set; } = "";
    public string? RejectionReason { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public string? Phone { get; set; }
    public List<ServiceDto> Services { get; set; } = new();
    public List<ReviewDto> Reviews { get; set; } = new();
}

public class UpdateProviderProfileRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string BusinessName { get; set; } = "";
    [StringLength(1000)] public string? Bio { get; set; }
    [Required] public int CategoryId { get; set; }
    [Required, StringLength(200)] public string ServiceArea { get; set; } = "";
    [Range(1, 1000000, ErrorMessage = "Hourly rate must be greater than 0.")] public decimal HourlyRate { get; set; }
    [Range(0, 60)] public int ExperienceYears { get; set; }
}

public class AvailabilityRequest
{
    public bool IsAvailable { get; set; }
    [Required] public string AvailableDays { get; set; } = "";
    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Start time must be HH:mm.")] public string StartTime { get; set; } = "09:00";
    [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "End time must be HH:mm.")] public string EndTime { get; set; } = "18:00";
}

// ---------- Bookings / Reviews / Reports ----------
public class CreateBookingRequest
{
    [Required] public int ServiceId { get; set; }
    [Required] public DateTime ScheduledDate { get; set; }
    [Required, StringLength(300, MinimumLength = 5)] public string Address { get; set; } = "";
    [StringLength(1000)] public string? Notes { get; set; }
}

public class BookingDto
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public DateTime ScheduledDate { get; set; }
    public string Address { get; set; } = "";
    public string? Notes { get; set; }
    public decimal EstimatedPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ServiceId { get; set; }
    public string ServiceTitle { get; set; } = "";
    public string PriceUnit { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = "";
    public string? ProviderPhone { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string? CustomerPhone { get; set; }
    public bool CanReview { get; set; }
    public ReviewDto? Review { get; set; }
}

public class ReviewDto
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public int ProviderId { get; set; }
    public string ProviderName { get; set; } = "";
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = "";
    public string ServiceTitle { get; set; } = "";
    public int Rating { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewRequest
{
    [Required] public int BookingId { get; set; }
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")] public int Rating { get; set; }
    [Required, StringLength(1000, MinimumLength = 3, ErrorMessage = "Please write a short review (3-1000 characters).")] public string Comment { get; set; } = "";
}

public class CreateReportRequest
{
    [Required] public int ReportedUserId { get; set; }
    public int? ReviewId { get; set; }
    [Required, StringLength(150)] public string Reason { get; set; } = "";
    [StringLength(1000)] public string? Details { get; set; }
}

public class ReportDto
{
    public int Id { get; set; }
    public int ReporterId { get; set; }
    public string ReporterName { get; set; } = "";
    public int ReportedUserId { get; set; }
    public string ReportedUserName { get; set; } = "";
    public bool ReportedUserActive { get; set; }
    public int? ReviewId { get; set; }
    public string? ReviewComment { get; set; }
    public string Reason { get; set; } = "";
    public string? Details { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

// ---------- Search / AI ----------
public class SearchQuery
{
    public string? Q { get; set; }
    public int? CategoryId { get; set; }
    public string? Location { get; set; }
    public decimal? MinRate { get; set; }
    public decimal? MaxRate { get; set; }
    public DateOnly? Date { get; set; }
    public bool AvailableOnly { get; set; }
    public int? MinRating { get; set; }
}

public class AiSearchRequest
{
    [Required, StringLength(500, MinimumLength = 3)] public string Query { get; set; } = "";
}

public class ParsedQuery
{
    public string? ServiceType { get; set; }
    public string? Location { get; set; }
    public decimal? MaxBudget { get; set; }
    public DateOnly? Date { get; set; }
}

public class AiSearchResult
{
    public string Query { get; set; } = "";
    public ParsedQuery Parsed { get; set; } = new();
    public bool UsedAi { get; set; }
    public string Parser { get; set; } = "";
    public string? Note { get; set; }
    public List<ProviderDto> Results { get; set; } = new();
}

// ---------- Admin ----------
public class DashboardStatsDto
{
    public int TotalCustomers { get; set; }
    public int TotalProviders { get; set; }
    public int PendingApprovals { get; set; }
    public int ApprovedProviders { get; set; }
    public int TotalServices { get; set; }
    public int TotalCategories { get; set; }
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int TotalReviews { get; set; }
    public int OpenReports { get; set; }
    public Dictionary<string, int> BookingsByStatus { get; set; } = new();
}

public class AdminUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? Phone { get; set; }
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ProviderStatus { get; set; }
}

public class RejectRequest
{
    [StringLength(300)] public string? Reason { get; set; }
}

public class ActivityDto
{
    public string Type { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime At { get; set; }
}
