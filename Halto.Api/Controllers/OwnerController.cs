using Halto.Application.DTOs.Staff;
using Halto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halto.Api.Controllers;

[Route("api/owner")]
[Authorize(Roles = "OrganizationOwner")]
public class OwnerController : HaltoControllerBase
{
    private readonly IStaffService _staffService;

    public OwnerController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    /// <summary>Create a staff account for this organization</summary>
    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var result = await _staffService.CreateStaffAsync(CurrentOrgId, request);
        return ToActionResult(result);
    }

    /// <summary>List all staff in this organization</summary>
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        var result = await _staffService.GetStaffAsync(CurrentOrgId);
        return ToActionResult(result);
    }

    /// <summary>Activate or deactivate a staff member</summary>
    [HttpPatch("staff/{id:guid}/status")]
    public async Task<IActionResult> UpdateStaffStatus(Guid id, [FromBody] UpdateStaffStatusRequest request)
    {
        var result = await _staffService.UpdateStaffStatusAsync(CurrentOrgId, id, request.IsActive);
        return ToActionResult(result);
    }
}

// DTO defined inline since it's tiny
public class UpdateStaffStatusRequest
{
    public bool IsActive { get; set; }
}
