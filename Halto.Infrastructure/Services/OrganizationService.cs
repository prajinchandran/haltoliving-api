using Halto.Application.Common;
using Halto.Application.DTOs.Organizations;
using Halto.Application.Interfaces;
using Halto.Domain.Entities;
using Halto.Domain.Enums;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Services;

public class OrganizationService : IOrganizationService
{
    private readonly HaltoDbContext _db;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(HaltoDbContext db, ILogger<OrganizationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<OrganizationDto>> CreateAsync(CreateOrganizationWithOwnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            return Result<OrganizationDto>.Failure("Organization name is required.");

        if (string.IsNullOrWhiteSpace(request.OwnerEmail) || string.IsNullOrWhiteSpace(request.OwnerPassword))
            return Result<OrganizationDto>.Failure("Owner email and password are required.");

        var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == request.OwnerEmail.ToLower());
        if (emailExists)
            return Result<OrganizationDto>.Failure("Owner email already registered.");

        var org = new Organization
        {
            Name = request.OrganizationName.Trim(),
            BusinessType = request.BusinessType,
            IsActive = true
        };
        _db.Organizations.Add(org);

        var owner = new User
        {
            Email = request.OwnerEmail.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.OwnerPassword),
            FullName = request.OwnerFullName.Trim(),
            Phone = request.OwnerPhone?.Trim(),
            Role = UserRole.OrganizationOwner,
            IsActive = true,
            OrganizationId = org.Id
        };
        _db.Users.Add(owner);

        await _db.SaveChangesAsync();
        _logger.LogInformation("Organization created: {OrgName} by SuperAdmin", org.Name);

        return Result<OrganizationDto>.Success(new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            BusinessType = org.BusinessType.ToString(),
            IsActive = org.IsActive,
            CreatedAt = org.CreatedAt,
            OwnerName = owner.FullName,
            OwnerEmail = owner.Email,
            MemberCount = 0
        }, 201);
    }

    public async Task<Result<List<OrganizationDto>>> GetAllAsync()
    {
        var orgs = await _db.Organizations
            .Select(o => new
            {
                o.Id, o.Name, o.BusinessType, o.IsActive, o.CreatedAt,
                MemberCount = o.Members.Count(m => m.IsActive && m.DiscontinuedAt == null),
                OwnerName = o.Users.Where(u => u.Role == UserRole.OrganizationOwner).Select(u => u.FullName).FirstOrDefault(),
                OwnerEmail = o.Users.Where(u => u.Role == UserRole.OrganizationOwner).Select(u => u.Email).FirstOrDefault(),
                TotalRevenue = o.Payments.Sum(p => (decimal?)p.AmountPaid) ?? 0,
                TotalDue = o.Dues.Sum(d => (decimal?)d.Amount) ?? 0,
                DueCount = o.Dues.Count(),
                PaidCount = o.Dues.Count(d => d.Status == DueStatus.Paid),
                PartialCount = o.Dues.Count(d => d.Status == DueStatus.Partial),
            })
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var result = orgs.Select(o => new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            BusinessType = o.BusinessType.ToString(),
            IsActive = o.IsActive,
            CreatedAt = o.CreatedAt,
            MemberCount = o.MemberCount,
            OwnerName = o.OwnerName,
            OwnerEmail = o.OwnerEmail,
            TotalRevenue = o.TotalRevenue,
            TotalDue = o.TotalDue,
            TotalOutstanding = o.TotalDue - o.TotalRevenue,
            DueCount = o.DueCount,
            PaidCount = o.PaidCount,
            PartialCount = o.PartialCount,
        }).ToList();

        return Result<List<OrganizationDto>>.Success(result);
    }

    public async Task<Result<OrganizationDto>> GetByIdAsync(Guid id)
    {
        var o = await _db.Organizations
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.Id, o.Name, o.BusinessType, o.IsActive, o.CreatedAt,
                MemberCount = o.Members.Count(m => m.IsActive && m.DiscontinuedAt == null),
                OwnerName = o.Users.Where(u => u.Role == UserRole.OrganizationOwner).Select(u => u.FullName).FirstOrDefault(),
                OwnerEmail = o.Users.Where(u => u.Role == UserRole.OrganizationOwner).Select(u => u.Email).FirstOrDefault(),
                TotalRevenue = o.Payments.Sum(p => (decimal?)p.AmountPaid) ?? 0,
                TotalDue = o.Dues.Sum(d => (decimal?)d.Amount) ?? 0,
                DueCount = o.Dues.Count(),
                PaidCount = o.Dues.Count(d => d.Status == DueStatus.Paid),
                PartialCount = o.Dues.Count(d => d.Status == DueStatus.Partial),
            })
            .FirstOrDefaultAsync();

        if (o is null)
            return Result<OrganizationDto>.NotFound("Organization not found.");

        return Result<OrganizationDto>.Success(new OrganizationDto
        {
            Id = o.Id,
            Name = o.Name,
            BusinessType = o.BusinessType.ToString(),
            IsActive = o.IsActive,
            CreatedAt = o.CreatedAt,
            MemberCount = o.MemberCount,
            OwnerName = o.OwnerName,
            OwnerEmail = o.OwnerEmail,
            TotalRevenue = o.TotalRevenue,
            TotalDue = o.TotalDue,
            TotalOutstanding = o.TotalDue - o.TotalRevenue,
            DueCount = o.DueCount,
            PaidCount = o.PaidCount,
            PartialCount = o.PartialCount,
        });
    }

    public async Task<Result<OrganizationDto>> UpdateStatusAsync(Guid id, bool isActive)
    {
        var org = await _db.Organizations.FindAsync(id);
        if (org is null)
            return Result<OrganizationDto>.NotFound("Organization not found.");

        org.IsActive = isActive;
        org.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<Result<SuperAdminSummaryDto>> GetSuperAdminSummaryAsync()
    {
        var orgs = await _db.Organizations.CountAsync();
        var activeOrgs = await _db.Organizations.CountAsync(o => o.IsActive);
        var totalMembers = await _db.Members.CountAsync(m => m.IsActive && m.DiscontinuedAt == null);
        var totalRevenue = await _db.Payments.SumAsync(p => (decimal?)p.AmountPaid) ?? 0;
        var totalDue = await _db.Dues.SumAsync(d => (decimal?)d.Amount) ?? 0;

        var top = await _db.Organizations
            .Select(o => new OrgRevenueDto
            {
                Id = o.Id,
                Name = o.Name,
                BusinessType = o.BusinessType.ToString(),
                Revenue = o.Payments.Sum(p => (decimal?)p.AmountPaid) ?? 0,
                MemberCount = o.Members.Count(m => m.IsActive && m.DiscontinuedAt == null),
            })
            .OrderByDescending(o => o.Revenue)
            .Take(5)
            .ToListAsync();

        return Result<SuperAdminSummaryDto>.Success(new SuperAdminSummaryDto
        {
            TotalOrganizations = orgs,
            ActiveOrganizations = activeOrgs,
            TotalMembers = totalMembers,
            TotalRevenue = totalRevenue,
            TotalOutstanding = totalDue - totalRevenue,
            TopOrganizations = top
        });
    }
}
