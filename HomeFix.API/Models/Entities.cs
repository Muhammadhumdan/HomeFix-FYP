using System.ComponentModel.DataAnnotations;

namespace HomeFix.API.Models;

public enum UserRole { Customer, Provider, Admin }
public enum ApprovalStatus { Pending, Approved, Rejected }
public enum BookingStatus { Pending, Accepted, Rejected, Completed, Cancelled }
public enum ReportStatus { Open, Resolved, Dismissed }

public class User
{
    public int Id { get; set; }
    [MaxLength(100)] public string FullName { get; set; } = "";
    [MaxLength(200)] public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public UserRole Role { get; set; }
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(100)] public string? City { get; set; }
    [MaxLength(300)] public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ProviderProfile? ProviderProfile { get; set; }
}

public class ServiceCategory
{
    public int Id { get; set; }
    [MaxLength(80)] public string Name { get; set; } = "";
    [MaxLength(300)] public string? Description { get; set; }
    [MaxLength(10)] public string? Icon { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ProviderProfile> Providers { get; set; } = new();
    public List<Service> Services { get; set; } = new();
}

public class ProviderProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(150)] public string BusinessName { get; set; } = "";
    [MaxLength(1000)] public string? Bio { get; set; }
    public int CategoryId { get; set; }
    public ServiceCategory Category { get; set; } = null!;
    [MaxLength(200)] public string ServiceArea { get; set; } = "";
    public decimal HourlyRate { get; set; }
    public int ExperienceYears { get; set; }
    public bool IsAvailable { get; set; } = true;
    [MaxLength(60)] public string AvailableDays { get; set; } = "Mon,Tue,Wed,Thu,Fri,Sat";
    [MaxLength(5)] public string StartTime { get; set; } = "09:00";
    [MaxLength(5)] public string EndTime { get; set; } = "18:00";
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
    [MaxLength(300)] public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Service> Services { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new();
    public List<Review> Reviews { get; set; } = new();
}

public class Service
{
    public int Id { get; set; }
    public int ProviderProfileId { get; set; }
    public ProviderProfile ProviderProfile { get; set; } = null!;
    public int CategoryId { get; set; }
    public ServiceCategory Category { get; set; } = null!;
    [MaxLength(150)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string? Description { get; set; }
    public decimal Price { get; set; }
    [MaxLength(20)] public string PriceUnit { get; set; } = "hour";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Booking
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public int ProviderProfileId { get; set; }
    public ProviderProfile ProviderProfile { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public DateTime ScheduledDate { get; set; }
    [MaxLength(300)] public string Address { get; set; } = "";
    [MaxLength(1000)] public string? Notes { get; set; }
    public decimal EstimatedPrice { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Review? Review { get; set; }
}

public class Review
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public int ProviderProfileId { get; set; }
    public ProviderProfile ProviderProfile { get; set; } = null!;
    public int Rating { get; set; }
    [MaxLength(1000)] public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Report
{
    public int Id { get; set; }
    public int ReporterId { get; set; }
    public User Reporter { get; set; } = null!;
    public int ReportedUserId { get; set; }
    public User ReportedUser { get; set; } = null!;
    public int? ReviewId { get; set; }
    public Review? Review { get; set; }
    [MaxLength(150)] public string Reason { get; set; } = "";
    [MaxLength(1000)] public string? Details { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
