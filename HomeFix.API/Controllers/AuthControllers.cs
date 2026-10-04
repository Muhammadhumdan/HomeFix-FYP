using HomeFix.API.Authentication;
using HomeFix.API.DTOs;
using HomeFix.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeFix.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    /// <summary>Register a Customer or a Provider (providers must be approved by an Admin).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request));

    /// <summary>Login and receive a JWT.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request) => Ok(await auth.LoginAsync(request));
}

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IAuthService auth) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me() => Ok(await auth.GetMeAsync(User.GetUserId()));

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest request) =>
        Ok(await auth.UpdateMeAsync(User.GetUserId(), request));
}
