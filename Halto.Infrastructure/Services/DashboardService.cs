using Halto.Application.Common;
using Halto.Application.DTOs.Dashboard;
using Halto.Application.Interfaces;
using Halto.Domain.Enums;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Halto.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly HaltoDbContext _db;

    public DashboardService(HaltoDbContext db)
    {
        _db = db;
    }

    public async Task<Result<DashboardSummaryDto>> GetSummaryAsync(Guid organizationId, DateTime? from, DateTime? to)
    {
        var dueQuery = _db.Dues.Where(d => d.OrganizationId == organizationId);

        if (from.HasValue)
        {
            var f = from.Value;
            dueQuery = dueQuery.Where(d => d.Year > f.Year || (d.Year == f.Year && d.Month >= f.Month));
        }
        if (to.HasValue)
        {
            var t = to.Value;
            dueQuery = dueQuery.Where(d => d.Year < t.Year || (d.Year == t.Year && d.Month <= t.Month));
        }

        var dues = await dueQuery.Include(d => d.Payments).ToListAsync();

        var totalDue = dues.Sum(d => d.Amount);
        var totalPaid = dues.Sum(d => d.Payments.Sum(p => p.AmountPaid));
        var paidCount = dues.Count(d => d.Status == DueStatus.Paid);
        var partialCount = dues.Count(d => d.Status == DueStatus.Partial);

        var memberCount = await _db.Members.CountAsync(m => m.OrganizationId == organizationId && m.IsActive);

        return Result<DashboardSummaryDto>.Success(new DashboardSummaryDto
        {
            TotalDueAmount = totalDue,
            TotalPaidAmount = totalPaid,
            TotalUnpaidAmount = totalDue - totalPaid,
            MemberCount = memberCount,
            DueCount = dues.Count,
            PaidCount = paidCount,
            PartialCount = partialCount,
            UnpaidCount = dues.Count - paidCount - partialCount
        });
    }

    public async Task<Result<List<MonthlyChartDto>>> GetMonthlyChartAsync(Guid organizationId, int year)
    {
        var dues = await _db.Dues
            .Include(d => d.Payments)
            .Where(d => d.OrganizationId == organizationId && d.Year == year)
            .ToListAsync();

        var monthNames = new[] { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                      "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        var result = Enumerable.Range(1, 12).Select(m =>
        {
            var monthDues = dues.Where(d => d.Month == m).ToList();
            var totalDue = monthDues.Sum(d => d.Amount);
            var totalPaid = monthDues.Sum(d => d.Payments.Sum(p => p.AmountPaid));
            return new MonthlyChartDto
            {
                Month = m,
                MonthName = monthNames[m],
                TotalDue = totalDue,
                TotalPaid = totalPaid,
                Balance = totalDue - totalPaid,
                DueCount = monthDues.Count,
                PaidCount = monthDues.Count(d => d.Status == DueStatus.Paid)
            };
        }).ToList();

        return Result<List<MonthlyChartDto>>.Success(result);
    }

    public async Task<Result<PagedResult<MemberDueStatusDto>>> GetMembersDueStatusAsync(
        Guid organizationId, int year, int month, string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var memberQuery = _db.Members.Where(m => m.OrganizationId == organizationId && m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            memberQuery = memberQuery.Where(m =>
                m.FullName.ToLower().Contains(s) ||
                (m.Phone != null && m.Phone.Contains(s)) ||
                (m.Email != null && m.Email.ToLower().Contains(s)));
        }

        var total = await memberQuery.CountAsync();
        var members = await memberQuery
            .OrderBy(m => m.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var memberIds = members.Select(m => m.Id).ToList();

        var dues = await _db.Dues
            .Include(d => d.Payments)
            .Where(d => d.OrganizationId == organizationId && d.Year == year && d.Month == month && memberIds.Contains(d.MemberId))
            .ToListAsync();

        var lastPayments = await _db.Payments
            .Where(p => p.OrganizationId == organizationId && memberIds.Contains(p.MemberId))
            .GroupBy(p => p.MemberId)
            .Select(g => new { MemberId = g.Key, LastPaid = g.Max(p => p.PaidOn) })
            .ToListAsync();

        var items = members.Select(m =>
        {
            var due = dues.FirstOrDefault(d => d.MemberId == m.Id);
            var dueAmt = due?.Amount ?? 0;
            var paidAmt = due?.Payments.Sum(p => p.AmountPaid) ?? 0;
            var lastPaid = lastPayments.FirstOrDefault(lp => lp.MemberId == m.Id)?.LastPaid;

            return new MemberDueStatusDto
            {
                MemberId = m.Id,
                MemberName = m.FullName,
                Phone = m.Phone,
                Email = m.Email,
                DueAmount = dueAmt,
                PaidAmount = paidAmt,
                Balance = dueAmt - paidAmt,
                Status = due?.Status.ToString() ?? "NoDue",
                LastPaidDate = lastPaid
            };
        }).ToList();

        return Result<PagedResult<MemberDueStatusDto>>.Success(new PagedResult<MemberDueStatusDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<List<UpcomingDueDto>>> GetUpcomingDuesAsync(Guid organizationId, int daysAhead)
    {
        // "Upcoming dues" = dues for next month (or current month if before day 10)
        // that are either already generated and unpaid, OR not yet generated for active members
        var now = DateTime.UtcNow;
        var cutoffDay = 10;

        // Determine which month to show: if today <= cutoff day, show current month; else show next month
        int targetYear, targetMonth;
        if (now.Day <= cutoffDay)
        {
            targetYear = now.Year;
            targetMonth = now.Month;
        }
        else
        {
            var next = now.AddMonths(1);
            targetYear = next.Year;
            targetMonth = next.Month;
        }

        var monthNames = new[] { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                      "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        // Get all active members with their category
        var members = await _db.Members
            .Include(m => m.Category)
            .Where(m => m.OrganizationId == organizationId && m.IsActive)
            .ToListAsync();

        // Get existing dues for target month
        var existingDues = await _db.Dues
            .Include(d => d.Payments)
            .Where(d => d.OrganizationId == organizationId && d.Year == targetYear && d.Month == targetMonth)
            .ToListAsync();

        var result = members.Select(m =>
        {
            var due = existingDues.FirstOrDefault(d => d.MemberId == m.Id);
            var amount = due?.Amount ?? m.Category?.MonthlyRent ?? 0;
            var totalPaid = due?.Payments.Sum(p => p.AmountPaid) ?? 0;

            return new UpcomingDueDto
            {
                MemberId = m.Id,
                MemberName = m.FullName,
                Phone = m.Phone,
                Email = m.Email,
                CategoryName = m.Category?.Name,
                Amount = amount,
                DueYear = targetYear,
                DueMonth = targetMonth,
                DueMonthName = monthNames[targetMonth],
                DueId = due?.Id,
                Status = due?.Status.ToString(),
                TotalPaid = totalPaid,
                Balance = amount - totalPaid
            };
        })
        .Where(u => u.Amount > 0)
        .OrderBy(u => u.Status == "Paid" ? 1 : 0)  // unpaid first
        .ThenBy(u => u.MemberName)
        .ToList();

        return Result<List<UpcomingDueDto>>.Success(result);
    }
}
