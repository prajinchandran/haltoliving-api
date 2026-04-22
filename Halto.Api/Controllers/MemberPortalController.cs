using Halto.Application.Interfaces;
using Halto.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Halto.Api.Controllers;

/// <summary>
/// Member-facing endpoints. Members can only view their own dues and payment history.
/// The member's User record links to a Member record via Member.UserId.
/// </summary>
[Route("api/member")]
[Authorize(Roles = "Member")]
public class MemberPortalController : HaltoControllerBase
{
    private readonly IDueService _dueService;
    private readonly IPaymentService _paymentService;
    private readonly HaltoDbContext _db;

    public MemberPortalController(IDueService dueService, IPaymentService paymentService, HaltoDbContext db)
    {
        _dueService = dueService;
        _paymentService = paymentService;
        _db = db;
    }

    /// <summary>Get all dues for the authenticated member</summary>
    [HttpGet("dues")]
    public async Task<IActionResult> GetMyDues([FromQuery] int? year, [FromQuery] int? month, [FromQuery] string? status)
    {
        var memberId = await GetMemberIdAsync();
        if (memberId == null)
            return NotFound(new { success = false, error = "Member profile not found for this account." });

        var result = await _dueService.GetDuesAsync(CurrentOrgId, memberId, year, month, status);
        return ToActionResult(result);
    }

    /// <summary>Get payment history for the authenticated member</summary>
    [HttpGet("payments")]
    public async Task<IActionResult> GetMyPayments([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var memberId = await GetMemberIdAsync();
        if (memberId == null)
            return NotFound(new { success = false, error = "Member profile not found for this account." });

        var result = await _paymentService.GetPaymentsAsync(CurrentOrgId, memberId, from, to);
        return ToActionResult(result);
    }

    private async Task<Guid?> GetMemberIdAsync()
    {
        var member = await _db.Members
            .Where(m => m.UserId == CurrentUserId && m.OrganizationId == CurrentOrgId)
            .Select(m => m.Id)
            .FirstOrDefaultAsync();

        return member == Guid.Empty ? null : member;
    }
}
