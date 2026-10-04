using HomeFix.API.Authentication;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeFix.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService categories) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<List<CategoryDto>>> GetActive() => Ok(await categories.GetAllAsync(false));

    [HttpGet("all"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<CategoryDto>>> GetAll() => Ok(await categories.GetAllAsync(true));

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest r) => StatusCode(201, await categories.CreateAsync(r));

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest r) => Ok(await categories.UpdateAsync(id, r));

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id) { await categories.DeleteAsync(id); return NoContent(); }
}

[ApiController]
[Route("api/providers")]
public class ProvidersController(IProviderService providers, ISearchService search, IReviewService reviews) : ControllerBase
{
    /// <summary>Search/filter approved providers (q, categoryId, location, minRate, maxRate, date, availableOnly, minRating).</summary>
    [HttpGet] public async Task<ActionResult<List<ProviderDto>>> Search([FromQuery] SearchQuery query) =>
        Ok(await search.SearchProvidersAsync(query));

    [HttpGet("featured")] public async Task<ActionResult<List<ProviderDto>>> Featured([FromQuery] int count = 6) =>
        Ok(await providers.GetFeaturedAsync(Math.Clamp(count, 1, 20)));

    [HttpGet("{id:int}")] public async Task<ActionResult<ProviderDto>> Get(int id) => Ok(await providers.GetPublicAsync(id));

    [HttpGet("{id:int}/reviews")] public async Task<ActionResult<List<ReviewDto>>> GetReviews(int id) =>
        Ok(await reviews.GetForProviderAsync(id));

    [HttpGet("me"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<ProviderDto>> Mine() => Ok(await providers.GetMineAsync(User.GetUserId()));

    [HttpPut("me"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<ProviderDto>> UpdateMine(UpdateProviderProfileRequest r) =>
        Ok(await providers.UpdateMineAsync(User.GetUserId(), r));

    [HttpPut("me/availability"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<ProviderDto>> UpdateAvailability(AvailabilityRequest r) =>
        Ok(await providers.UpdateAvailabilityAsync(User.GetUserId(), r));
}

[ApiController]
[Route("api/services")]
public class ServicesController(IServiceListingService services) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<List<ServiceDto>>> GetPublic([FromQuery] int? categoryId, [FromQuery] string? q) =>
        Ok(await services.GetPublicAsync(categoryId, q));

    [HttpGet("mine"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<List<ServiceDto>>> Mine() => Ok(await services.GetMineAsync(User.GetUserId()));

    [HttpGet("mine/{id:int}"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<ServiceDto>> MineById(int id) => Ok(await services.GetMineByIdAsync(User.GetUserId(), id));

    [HttpPost, Authorize(Roles = "Provider")]
    public async Task<ActionResult<ServiceDto>> Create(ServiceRequest r) => StatusCode(201, await services.CreateAsync(User.GetUserId(), r));

    [HttpPut("{id:int}"), Authorize(Roles = "Provider")]
    public async Task<ActionResult<ServiceDto>> Update(int id, ServiceRequest r) => Ok(await services.UpdateAsync(User.GetUserId(), id, r));

    [HttpDelete("{id:int}"), Authorize(Roles = "Provider")]
    public async Task<IActionResult> Delete(int id) { await services.DeleteAsync(User.GetUserId(), id); return NoContent(); }
}
