using HomeFix.API.DTOs;
using HomeFix.API.Models;

namespace HomeFix.API.Interfaces;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) Create(User user);
}

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<UserDto> GetMeAsync(int userId);
    Task<UserDto> UpdateMeAsync(int userId, UpdateProfileRequest request);
}

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(bool includeInactive);
    Task<CategoryDto> CreateAsync(CategoryRequest request);
    Task<CategoryDto> UpdateAsync(int id, CategoryRequest request);
    Task DeleteAsync(int id);
}

public interface IProviderService
{
    Task<ProviderDto> GetPublicAsync(int id);
    Task<ProviderDto> GetMineAsync(int userId);
    Task<ProviderDto> UpdateMineAsync(int userId, UpdateProviderProfileRequest request);
    Task<ProviderDto> UpdateAvailabilityAsync(int userId, AvailabilityRequest request);
    Task<List<ProviderDto>> GetFeaturedAsync(int count);
}

public interface IServiceListingService
{
    Task<List<ServiceDto>> GetPublicAsync(int? categoryId, string? q);
    Task<List<ServiceDto>> GetMineAsync(int userId);
    Task<ServiceDto> GetMineByIdAsync(int userId, int id);
    Task<ServiceDto> CreateAsync(int userId, ServiceRequest request);
    Task<ServiceDto> UpdateAsync(int userId, int id, ServiceRequest request);
    Task DeleteAsync(int userId, int id);
}

public interface IBookingService
{
    Task<BookingDto> CreateAsync(int customerId, CreateBookingRequest request);
    Task<List<BookingDto>> GetMineAsync(int userId, UserRole role, string? status);
    Task<BookingDto> GetByIdAsync(int userId, UserRole role, int id);
    Task<BookingDto> ChangeStatusAsync(int userId, UserRole role, int id, BookingStatus target);
}

public interface IReviewService
{
    Task<ReviewDto> CreateAsync(int customerId, CreateReviewRequest request);
    Task<List<ReviewDto>> GetForProviderAsync(int providerId);
    Task<List<ReviewDto>> GetMineAsync(int userId, UserRole role);
}

public interface ISearchService
{
    Task<List<ProviderDto>> SearchProvidersAsync(SearchQuery query);
}

public interface IQueryParser
{
    string Name { get; }
    Task<ParsedQuery> ParseAsync(string text, IReadOnlyList<string> categories, IReadOnlyList<string> locations, DateOnly today);
}

public interface IAiSearchService
{
    Task<AiSearchResult> SearchAsync(string query);
}

public interface IAdminService
{
    Task<DashboardStatsDto> GetStatsAsync();
    Task<List<AdminUserDto>> GetUsersAsync(string? role);
    Task<List<ProviderDto>> GetProvidersAsync(string? status);
    Task<ProviderDto> ApproveProviderAsync(int providerId);
    Task<ProviderDto> RejectProviderAsync(int providerId, string? reason);
    Task<AdminUserDto> ToggleUserActiveAsync(int adminId, int userId);
    Task<List<BookingDto>> GetBookingsAsync(string? status);
    Task<List<ActivityDto>> GetActivityAsync();
    Task<List<ReportDto>> GetReportsAsync(string? status);
    Task<ReportDto> CreateReportAsync(int reporterId, CreateReportRequest request);
    Task<ReportDto> SetReportStatusAsync(int id, ReportStatus status);
}
