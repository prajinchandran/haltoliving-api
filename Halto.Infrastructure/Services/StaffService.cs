using Halto.Application.Common;
using Halto.Application.DTOs.Staff;
using Halto.Application.Interfaces;
using Halto.Domain.Entities;
using Halto.Domain.Enums;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly HaltoDbContext _db;
    private readonly ILogger<StaffService> _logger;

    public StaffService(HaltoDbContext db, ILogger<StaffService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<StaffDto>> CreateStaffAsync(Guid organizationId, CreateStaffRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<StaffDto>.Failure("Email and password are required.");

        if (request.Password.Length < 6)
            return Result<StaffDto>.Failure("Password must be at least 6 characters.");

        var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailExists)
            return Result<StaffDto>.Failure("Email already registered.");

        var staff = new User
        {
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            Role = UserRole.OrganizationStaff,
            IsActive = true,
            OrganizationId = organizationId
        };
        _db.Users.Add(staff);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Staff created: {Email} for org: {OrgId}", staff.Email, organizationId);
        return Result<StaffDto>.Success(MapToDto(staff), 201);
    }

    public async Task<Result<List<StaffDto>>> GetStaffAsync(Guid organizationId)
    {
        var staff = await _db.Users
            .Where(u => u.OrganizationId == organizationId && u.Role == UserRole.OrganizationStaff)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new StaffDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Phone = u.Phone,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Result<List<StaffDto>>.Success(staff);
    }

    public async Task<Result<StaffDto>> UpdateStaffStatusAsync(Guid organizationId, Guid staffId, bool isActive)
    {
        var staff = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == staffId && u.OrganizationId == organizationId && u.Role == UserRole.OrganizationStaff);

        if (staff is null)
            return Result<StaffDto>.NotFound("Staff member not found.");

        staff.IsActive = isActive;
        staff.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Result<StaffDto>.Success(MapToDto(staff));
    }

    private static StaffDto MapToDto(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FullName = u.FullName,
        Phone = u.Phone,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt
    };
}
