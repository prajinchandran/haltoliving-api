using Halto.Application.DTOs.Members;
using Halto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halto.Api.Controllers;

[Route("api/members")]
[Authorize(Roles = "OrganizationOwner,OrganizationStaff")]
public class MembersController : HaltoControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    /// <summary>Add a new member to the organization</summary>
    [HttpPost]
    public async Task<IActionResult> CreateMember([FromBody] CreateMemberRequest request)
    {
        var result = await _memberService.CreateMemberAsync(CurrentOrgId, request);
        return ToActionResult(result);
    }

    /// <summary>List members with paging and optional search</summary>
    [HttpGet]
    public async Task<IActionResult> GetMembers(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _memberService.GetMembersAsync(CurrentOrgId, search, page, pageSize);
        return ToActionResult(result);
    }

    /// <summary>Get a single member by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetMember(Guid id)
    {
        var result = await _memberService.GetMemberByIdAsync(CurrentOrgId, id);
        return ToActionResult(result);
    }

    /// <summary>Update member details</summary>
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> UpdateMember(Guid id, [FromBody] UpdateMemberRequest request)
    {
        var result = await _memberService.UpdateMemberAsync(CurrentOrgId, id, request);
        return ToActionResult(result);
    }

    /// <summary>Create login credentials for a member (enables member portal access)</summary>
    [HttpPost("{id:guid}/create-login")]
    [Authorize(Roles = "OrganizationOwner")]
    public async Task<IActionResult> CreateMemberLogin(Guid id, [FromBody] CreateMemberLoginRequest request)
    {
        var result = await _memberService.CreateMemberLoginAsync(CurrentOrgId, id, request);
        return ToActionResult(result);
    }

    /// <summary>Discontinue a member — marks inactive with reason and timestamp</summary>
    [HttpPost("{id:guid}/discontinue")]
    [Authorize(Roles = "OrganizationOwner")]
    public async Task<IActionResult> Discontinue(Guid id, [FromBody] DiscontinueMemberRequest request)
    {
        var result = await _memberService.DiscontinueMemberAsync(CurrentOrgId, id, request.Reason);
        return ToActionResult(result);
    }
}
