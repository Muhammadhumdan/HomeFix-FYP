using HomeFix.API.Authentication;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using HomeFix.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeFix.API.Controllers;

[ApiController]
[Route("api/bookings")]
[Authorize]
public class BookingsController(IBookingService bookings) : ControllerBase
{
    [HttpPost, Authorize(Roles = "Customer")]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest r) =>
        StatusCode(201, await bookings.CreateAsync(User.GetUserId(), r));

    /// <summary>Customer: own bookings. Provider: bookings received. Admin: all bookings.</summary>
    [HttpGet] public async Task<ActionResult<List<BookingDto>>> Mine([FromQuery] string? status) =>
        Ok(await bookings.GetMineAsync(User.GetUserId(), User.GetRole(), status));

    [HttpGet("{id:int}")] public async Task<ActionResult<BookingDto>> Get(int id) =>
        Ok(await bookings.GetByIdAsync(User.GetUserId(), User.GetRole(), id));

    [HttpPost("{id:int}/accept"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<BookingDto>> Accept(int id) => await Change(id, BookingStatus.Accepted);

    [HttpPost("{id:int}/reject"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<BookingDto>> Reject(int id) => await Change(id, BookingStatus.Rejected);

    [HttpPost("{id:int}/complete"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<BookingDto>> Complete(int id) => await Change(id, BookingStatus.Completed);

    [HttpPost("{id:int}/cancel"), Authorize(Roles = "Customer")]
    public async Task<ActionResult<BookingDto>> Cancel(int id) => await Change(id, BookingStatus.Cancelled);

    private async Task<ActionResult<BookingDto>> Change(int id, BookingStatus target) =>
        Ok(await bookings.ChangeStatusAsync(User.GetUserId(), User.GetRole(), id, target));
}

[ApiController]
[Route("api/reviews")]
public class ReviewsController(IReviewService reviews) : ControllerBase
{
    [HttpPost, Authorize(Roles = "Customer")]
    public async Task<ActionResult<ReviewDto>> Create(CreateReviewRequest r) =>
        StatusCode(201, await reviews.CreateAsync(User.GetUserId(), r));

    [HttpGet("mine"), Authorize(Roles = "Customer,Provider")]
    public async Task<ActionResult<List<ReviewDto>>> Mine() => Ok(await reviews.GetMineAsync(User.GetUserId(), User.GetRole()));

    [HttpGet("provider/{providerId:int}")]
    public async Task<ActionResult<List<ReviewDto>>> ForProvider(int providerId) => Ok(await reviews.GetForProviderAsync(providerId));
}

[ApiController]
[Route("api/search")]
public class SearchController(ISearchService search, IAiSearchService ai) : ControllerBase
{
    [HttpGet("providers")] public async Task<ActionResult<List<ProviderDto>>> Providers([FromQuery] SearchQuery q) =>
        Ok(await search.SearchProvidersAsync(q));

    /// <summary>Natural-language search, e.g. "I need an electrician near Kohat tomorrow under Rs 3000".</summary>
    [HttpPost("ai")] public async Task<ActionResult<AiSearchResult>> Ai(AiSearchRequest r) => Ok(await ai.SearchAsync(r.Query));
}

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Customer,Provider")]
public class ReportsController(IAdminService admin) : ControllerBase
{
    [HttpPost] public async Task<ActionResult<ReportDto>> Create(CreateReportRequest r) =>
        StatusCode(201, await admin.CreateReportAsync(User.GetUserId(), r));
}

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService admin) : ControllerBase
{
    [HttpGet("stats")] public async Task<ActionResult<DashboardStatsDto>> Stats() => Ok(await admin.GetStatsAsync());
    [HttpGet("users")] public async Task<ActionResult<List<AdminUserDto>>> Users([FromQuery] string? role) => Ok(await admin.GetUsersAsync(role));
    [HttpGet("providers")] public async Task<ActionResult<List<ProviderDto>>> Providers([FromQuery] string? status) => Ok(await admin.GetProvidersAsync(status));
    [HttpPost("providers/{id:int}/approve")] public async Task<ActionResult<ProviderDto>> Approve(int id) => Ok(await admin.ApproveProviderAsync(id));
    [HttpPost("providers/{id:int}/reject")] public async Task<ActionResult<ProviderDto>> Reject(int id, RejectRequest? r) => Ok(await admin.RejectProviderAsync(id, r?.Reason));
    [HttpPost("users/{id:int}/toggle-active")] public async Task<ActionResult<AdminUserDto>> Toggle(int id) => Ok(await admin.ToggleUserActiveAsync(User.GetUserId(), id));
    [HttpGet("bookings")] public async Task<ActionResult<List<BookingDto>>> Bookings([FromQuery] string? status) => Ok(await admin.GetBookingsAsync(status));
    [HttpGet("activity")] public async Task<ActionResult<List<ActivityDto>>> Activity() => Ok(await admin.GetActivityAsync());
    [HttpGet("reports")] public async Task<ActionResult<List<ReportDto>>> Reports([FromQuery] string? status) => Ok(await admin.GetReportsAsync(status));
    [HttpPost("reports/{id:int}/resolve")] public async Task<ActionResult<ReportDto>> Resolve(int id) => Ok(await admin.SetReportStatusAsync(id, ReportStatus.Resolved));
    [HttpPost("reports/{id:int}/dismiss")] public async Task<ActionResult<ReportDto>> Dismiss(int id) => Ok(await admin.SetReportStatusAsync(id, ReportStatus.Dismissed));
}
