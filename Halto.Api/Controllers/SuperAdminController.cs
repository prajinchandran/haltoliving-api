using Halto.Application.DTOs.Organizations;
using Halto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halto.Api.Controllers;

[Route("api/super")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : HaltoControllerBase
{
    private readonly IOrganizationService _orgService;
    private readonly IAuthService _authService;

    public SuperAdminController(IOrganizationService orgService, IAuthService authService)
    {
        _orgService = orgService;
        _authService = authService;
    }

    /// <summary>Global summary across all organizations</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _orgService.GetSuperAdminSummaryAsync();
        return ToActionResult(result);
    }

    /// <summary>Create organization with owner account</summary>
    [HttpPost("organizations")]
    public async Task<IActionResult> CreateOrganization([FromBody] CreateOrganizationWithOwnerRequest request)
    {
        var result = await _orgService.CreateAsync(request);
        return ToActionResult(result);
    }

    /// <summary>List all organizations with revenue stats</summary>
    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations()
    {
        var result = await _orgService.GetAllAsync();
        return ToActionResult(result);
    }

    /// <summary>Get organization by ID</summary>
    [HttpGet("organizations/{id:guid}")]
    public async Task<IActionResult> GetOrganization(Guid id)
    {
        var result = await _orgService.GetByIdAsync(id);
        return ToActionResult(result);
    }

    /// <summary>Activate or deactivate an organization</summary>
    [HttpPatch("organizations/{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrganizationStatusRequest request)
    {
        var result = await _orgService.UpdateStatusAsync(id, request.IsActive);
        return ToActionResult(result);
    }
}
