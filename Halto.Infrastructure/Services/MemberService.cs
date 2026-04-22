using System.Text.Json;
using Halto.Application.Common;
using Halto.Application.DTOs.Auth;
using Halto.Application.DTOs.Members;
using Halto.Application.Interfaces;
using Halto.Domain.Entities;
using Halto.Domain.Enums;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Services;

public class MemberService : IMemberService
{
    private readonly HaltoDbContext _db;
    private readonly ILogger<MemberService> _logger;

    public MemberService(HaltoDbContext db, ILogger<MemberService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<MemberDto>> CreateMemberAsync(Guid organizationId, CreateMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return Result<MemberDto>.Failure("Full name is required.");

        // Validate category belongs to org
        if (request.CategoryId.HasValue)
        {
            var catExists = await _db.MemberCategories.AnyAsync(c =>
                c.Id == request.CategoryId.Value && c.OrganizationId == organizationId && c.IsActive);
            if (!catExists)
                return Result<MemberDto>.Failure("Invalid or inactive category.");
        }

        var member = new Member
        {
            FullName = request.FullName.Trim(),
            Email = request.Email?.ToLower().Trim(),
            Phone = request.Phone?.Trim(),
            Designation = request.Designation?.Trim(),
            IdDocumentType = request.IdDocumentType?.Trim(),
            IdDocumentUrl = request.IdDocumentUrl?.Trim(),
            CategoryId = request.CategoryId,
            JoinedAt = request.JoinedAt?.ToUniversalTime() ?? DateTime.UtcNow,
            OrganizationId = organizationId,
            IsActive = true,
            ExtraFieldsJson = request.ExtraFields.HasValue
                ? request.ExtraFields.Value.ValueKind != JsonValueKind.Undefined
                    ? request.ExtraFields.Value.GetRawText()
                    : null
                : null
        };

        _db.Members.Add(member);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Member created: {Name} in org: {OrgId}", member.FullName, organizationId);

        // Reload with category
        await _db.Entry(member).Reference(m => m.Category).LoadAsync();
        return Result<MemberDto>.Success(MapToDto(member), 201);
    }

    public async Task<Result<PagedResult<MemberDto>>> GetMembersAsync(Guid organizationId, string? search, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Members
            .Include(m => m.Category)
            .Where(m => m.OrganizationId == organizationId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(m =>
                m.FullName.ToLower().Contains(s) ||
                (m.Email != null && m.Email.ToLower().Contains(s)) ||
                (m.Phone != null && m.Phone.Contains(s)) ||
                (m.Designation != null && m.Designation.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Result<PagedResult<MemberDto>>.Success(new PagedResult<MemberDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<Result<MemberDto>> GetMemberByIdAsync(Guid organizationId, Guid memberId)
    {
        var member = await _db.Members
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == organizationId);

        if (member is null)
            return Result<MemberDto>.NotFound("Member not found.");

        return Result<MemberDto>.Success(MapToDto(member));
    }

    public async Task<Result<MemberDto>> UpdateMemberAsync(Guid organizationId, Guid memberId, UpdateMemberRequest request)
    {
        var member = await _db.Members
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == organizationId);

        if (member is null)
            return Result<MemberDto>.NotFound("Member not found.");

        if (!string.IsNullOrWhiteSpace(request.FullName)) member.FullName = request.FullName.Trim();
        if (request.Email is not null) member.Email = request.Email.ToLower().Trim();
        if (request.Phone is not null) member.Phone = request.Phone.Trim();
        if (request.Designation is not null) member.Designation = request.Designation.Trim();
        if (request.IdDocumentType is not null) member.IdDocumentType = request.IdDocumentType.Trim();
        if (request.IdDocumentUrl is not null) member.IdDocumentUrl = request.IdDocumentUrl.Trim();
        if (request.IsActive.HasValue) member.IsActive = request.IsActive.Value;

        if (request.CategoryId.HasValue)
        {
            var catExists = await _db.MemberCategories.AnyAsync(c =>
                c.Id == request.CategoryId.Value && c.OrganizationId == organizationId);
            if (!catExists) return Result<MemberDto>.Failure("Invalid category.");
            member.CategoryId = request.CategoryId.Value;
        }

        if (request.ExtraFields.HasValue && request.ExtraFields.Value.ValueKind != JsonValueKind.Undefined)
            member.ExtraFieldsJson = request.ExtraFields.Value.GetRawText();

        member.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _db.Entry(member).Reference(m => m.Category).LoadAsync();

        return Result<MemberDto>.Success(MapToDto(member));
    }

    public async Task<Result<MemberDto>> DiscontinueMemberAsync(Guid organizationId, Guid memberId, string? reason)
    {
        var member = await _db.Members
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == organizationId);

        if (member is null)
            return Result<MemberDto>.NotFound("Member not found.");

        if (!member.IsActive)
            return Result<MemberDto>.Failure("Member is already discontinued.");

        member.IsActive = false;
        member.DiscontinuedAt = DateTime.UtcNow;
        member.DiscontinuedReason = reason?.Trim();
        member.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Member discontinued: {Name} in org: {OrgId}", member.FullName, organizationId);

        return Result<MemberDto>.Success(MapToDto(member));
    }

    public async Task<Result<UserDto>> CreateMemberLoginAsync(Guid organizationId, Guid memberId, CreateMemberLoginRequest request)
    {
        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.Id == memberId && m.OrganizationId == organizationId);

        if (member is null)
            return Result<UserDto>.NotFound("Member not found.");

        if (member.UserId.HasValue)
            return Result<UserDto>.Failure("Member already has login credentials.");

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<UserDto>.Failure("Email and password are required.");

        var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailExists)
            return Result<UserDto>.Failure("Email already registered.");

        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = member.FullName,
            Phone = member.Phone,
            Role = UserRole.Member,
            IsActive = true,
            OrganizationId = organizationId
        };
        _db.Users.Add(user);

        member.UserId = user.Id;
        member.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Result<UserDto>.Success(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            OrganizationId = user.OrganizationId,
            CreatedAt = user.CreatedAt
        }, 201);
    }

    private static MemberDto MapToDto(Member m) => new()
    {
        Id = m.Id,
        FullName = m.FullName,
        Email = m.Email,
        Phone = m.Phone,
        Designation = m.Designation,
        IdDocumentType = m.IdDocumentType,
        IdDocumentUrl = m.IdDocumentUrl,
        IsActive = m.IsActive,
        JoinedAt = m.JoinedAt,
        DiscontinuedAt = m.DiscontinuedAt,
        DiscontinuedReason = m.DiscontinuedReason,
        CreatedAt = m.CreatedAt,
        OrganizationId = m.OrganizationId,
        CategoryId = m.CategoryId,
        CategoryName = m.Category?.Name,
        MonthlyRent = m.Category?.MonthlyRent,
        HasLogin = m.UserId.HasValue,
        UserId = m.UserId,
        ExtraFields = m.ExtraFieldsJson is not null
            ? JsonSerializer.Deserialize<object>(m.ExtraFieldsJson)
            : null
    };
}
