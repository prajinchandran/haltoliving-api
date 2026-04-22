using Halto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halto.Api.Controllers;

[Route("api/dashboard")]
[Authorize(Roles = "OrganizationOwner,OrganizationStaff")]
public class DashboardController : HaltoControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Summary totals for a date range.
    /// from/to are month boundaries (e.g. from=2024-01-01 means Jan 2024 onwards)
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _dashboardService.GetSummaryAsync(CurrentOrgId, from, to);
        return ToActionResult(result);
    }

    /// <summary>Month-wise due vs paid totals for a given year</summary>
    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthly([FromQuery] int? year)
    {
        var result = await _dashboardService.GetMonthlyChartAsync(CurrentOrgId, year ?? DateTime.UtcNow.Year);
        return ToActionResult(result);
    }

    /// <summary>Members with their due status for a specific month</summary>
    [HttpGet("members-due")]
    public async Task<IActionResult> GetMembersDue(
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var now = DateTime.UtcNow;
        var result = await _dashboardService.GetMembersDueStatusAsync(
            CurrentOrgId,
            year ?? now.Year,
            month ?? now.Month,
            search, page, pageSize);
        return ToActionResult(result);
    }

    /// <summary>Members with upcoming dues (within daysAhead, default 10)</summary>
    [HttpGet("upcoming-dues")]
    public async Task<IActionResult> GetUpcomingDues([FromQuery] int daysAhead = 10)
    {
        var result = await _dashboardService.GetUpcomingDuesAsync(CurrentOrgId, daysAhead);
        return ToActionResult(result);
    }
}
