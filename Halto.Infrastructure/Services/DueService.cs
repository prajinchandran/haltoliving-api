using System.Text.Json;
using Halto.Application.Common;
using Halto.Application.DTOs.Dues;
using Halto.Application.Interfaces;
using Halto.Domain.Entities;
using Halto.Domain.Enums;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Services;

public class DueService : IDueService
{
    private readonly HaltoDbContext _db;
    private readonly ILogger<DueService> _logger;

    public DueService(HaltoDbContext db, ILogger<DueService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<GenerateDuesResponse>> GenerateDuesAsync(Guid organizationId, Guid generatedByUserId, GenerateDuesRequest request)
    {
        if (request.Year < 2000 || request.Year > 2100)
            return Result<GenerateDuesResponse>.Failure("Invalid year.");
        if (request.Month < 1 || request.Month > 12)
            return Result<GenerateDuesResponse>.Failure("Month must be between 1 and 12.");

        // Get target members
        IQueryable<Member> memberQuery = _db.Members
            .Where(m => m.OrganizationId == organizationId && m.IsActive && m.DiscontinuedAt == null);

        if (request.MemberId.HasValue)
            memberQuery = memberQuery.Where(m => m.Id == request.MemberId.Value);

        var members = await memberQuery.ToListAsync();

        if (!members.Any())
            return Result<GenerateDuesResponse>.Failure("No active members found.");

        int generated = 0, skipped = 0;
        var skippedReasons = new List<string>();

        foreach (var member in members)
        {
            // Skip if due already exists for this month
            var exists = await _db.Dues.AnyAsync(d =>
                d.MemberId == member.Id && d.Year == request.Year && d.Month == request.Month);

            if (exists)
            {
                skipped++;
                skippedReasons.Add($"{member.FullName}: due already exists for {request.Year}/{request.Month:D2}");
                continue;
            }

            // Determine amount: override > extra fields > category monthly rent
            decimal? categoryRent = member.CategoryId.HasValue
                ? await _db.MemberCategories
                    .Where(c => c.Id == member.CategoryId.Value)
                    .Select(c => (decimal?)c.MonthlyRent)
                    .FirstOrDefaultAsync()
                : null;

            decimal amount = request.AmountOverride
            ?? (ExtractMonthlyAmount(member.ExtraFieldsJson) > 0 
                ? ExtractMonthlyAmount(member.ExtraFieldsJson) 
                : categoryRent)
            ?? 0;

            if (amount <= 0)
            {
                skipped++;
                skippedReasons.Add($"{member.FullName}: no amount configured (set AmountOverride or configure ExtraFields)");
                continue;
            }

            var due = new Due
            {
                OrganizationId = organizationId,
                MemberId = member.Id,
                Year = request.Year,
                Month = request.Month,
                Amount = amount,
                Status = DueStatus.Due,
                Notes = request.Notes
            };
            _db.Dues.Add(due);
            generated++;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Generated {Count} dues for org {OrgId} for {Year}/{Month}", generated, organizationId, request.Year, request.Month);

        return Result<GenerateDuesResponse>.Success(new GenerateDuesResponse
        {
            Generated = generated,
            Skipped = skipped,
            SkippedReasons = skippedReasons
        }, generated > 0 ? 201 : 200);
    }

    public async Task<Result<List<DueDto>>> GetDuesAsync(Guid organizationId, Guid? memberId, int? year, int? month, string? status)
    {
        var query = _db.Dues
            .Include(d => d.Member)
            .Include(d => d.Payments)
            .Where(d => d.OrganizationId == organizationId);

        if (memberId.HasValue) query = query.Where(d => d.MemberId == memberId.Value);
        if (year.HasValue) query = query.Where(d => d.Year == year.Value);
        if (month.HasValue) query = query.Where(d => d.Month == month.Value);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DueStatus>(status, true, out var s))
            query = query.Where(d => d.Status == s);

        var dues = await query.OrderByDescending(d => d.Year).ThenByDescending(d => d.Month).ToListAsync();
        return Result<List<DueDto>>.Success(dues.Select(MapToDto).ToList());
    }

    public async Task<Result<DueDto>> GetDueByIdAsync(Guid organizationId, Guid dueId)
    {
        var due = await _db.Dues
            .Include(d => d.Member)
            .Include(d => d.Payments)
            .FirstOrDefaultAsync(d => d.Id == dueId && d.OrganizationId == organizationId);

        if (due is null)
            return Result<DueDto>.NotFound("Due not found.");

        return Result<DueDto>.Success(MapToDto(due));
    }

    private static decimal ExtractMonthlyAmount(string? json)
    {
        if (string.IsNullOrEmpty(json)) return 0;
        try
        {
            using var doc = JsonDocument.Parse(json);
            // Try common field names for monthly amount
            foreach (var key in new[] { "monthlyRent", "monthlyFee", "amount", "fee", "rent" })
            {
                if (doc.RootElement.TryGetProperty(key, out var val) && val.TryGetDecimal(out var amt))
                    return amt;
            }
        }
        catch { /* ignore malformed JSON */ }
        return 0;
    }

    internal static DueDto MapToDto(Due d)
    {
        var totalPaid = d.Payments.Sum(p => p.AmountPaid);
        return new DueDto
        {
            Id = d.Id,
            MemberId = d.MemberId,
            MemberName = d.Member?.FullName ?? string.Empty,
            Year = d.Year,
            Month = d.Month,
            Amount = d.Amount,
            TotalPaid = totalPaid,
            Balance = d.Amount - totalPaid,
            Status = d.Status.ToString(),
            Notes = d.Notes,
            CreatedAt = d.CreatedAt,
            Payments = d.Payments.OrderBy(p => p.PaidOn).Select(p => new PaymentSummaryDto
            {
                Id = p.Id,
                AmountPaid = p.AmountPaid,
                PaidOn = p.PaidOn,
                Method = p.Method.ToString(),
                Notes = p.Notes
            }).ToList()
        };
    }
}
