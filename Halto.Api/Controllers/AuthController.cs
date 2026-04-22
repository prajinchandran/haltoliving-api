using Halto.Application.DTOs.Auth;
using Halto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halto.Api.Controllers;

[Route("api/auth")]
public class AuthController : HaltoControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return ToActionResult(result);
    }

    /// <summary>Self-service owner registration (creates org + owner in one step)</summary>
    [HttpPost("register-owner")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterOwner([FromBody] RegisterOwnerRequest request)
    {
        var result = await _authService.RegisterOwnerAsync(request);
        return ToActionResult(result);
    }

    /// <summary>Get current authenticated user profile</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var result = await _authService.GetMeAsync(CurrentUserId);
        return ToActionResult(result);
    }
}
