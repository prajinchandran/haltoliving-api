using Halto.Application.Common;
using Halto.Application.DTOs.Auth;
using Halto.Application.Interfaces;
using Halto.Domain.Entities;
using Halto.Domain.Enums;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly HaltoDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(HaltoDbContext db, ITokenService tokenService, ILogger<AuthService> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<LoginResponse>.Failure("Email and password are required.");

        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Result<LoginResponse>.Failure("Invalid email or password.", 401);

        if (!user.IsActive)
            return Result<LoginResponse>.Failure("Account is deactivated. Contact administrator.", 403);

        if (user.Organization is not null && !user.Organization.IsActive)
            return Result<LoginResponse>.Failure("Your organization is inactive. Contact platform support.", 403);

        var token = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Role.ToString(), user.OrganizationId);
        var refreshToken = _tokenService.GenerateRefreshToken();

        return Result<LoginResponse>.Success(new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = MapToUserDto(user)
        });
    }

    public async Task<Result<LoginResponse>> RegisterOwnerAsync(RegisterOwnerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Result<LoginResponse>.Failure("Email and password are required.");

        if (request.Password.Length < 6)
            return Result<LoginResponse>.Failure("Password must be at least 6 characters.");

        if (string.IsNullOrWhiteSpace(request.OrganizationName))
            return Result<LoginResponse>.Failure("Organization name is required.");

        var emailExists = await _db.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailExists)
            return Result<LoginResponse>.Failure("Email already registered.");

        var org = new Organization
        {
            Name = request.OrganizationName.Trim(),
            BusinessType = request.BusinessType,
            IsActive = true
        };
        _db.Organizations.Add(org);

        var owner = new User
        {
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            Phone = request.Phone?.Trim(),
            Role = UserRole.OrganizationOwner,
            IsActive = true,
            OrganizationId = org.Id
        };
        _db.Users.Add(owner);

        await _db.SaveChangesAsync();
        _logger.LogInformation("New owner registered: {Email} for org: {OrgName}", owner.Email, org.Name);

        var token = _tokenService.GenerateAccessToken(owner.Id, owner.Email, owner.Role.ToString(), owner.OrganizationId);
        var refreshToken = _tokenService.GenerateRefreshToken();

        owner.Organization = org;
        return Result<LoginResponse>.Success(new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = MapToUserDto(owner)
        }, 201);
    }

    public async Task<Result<UserDto>> GetMeAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return Result<UserDto>.NotFound("User not found.");

        return Result<UserDto>.Success(MapToUserDto(user));
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Phone = user.Phone,
        Role = user.Role.ToString(),
        IsActive = user.IsActive,
        OrganizationId = user.OrganizationId,
        OrganizationName = user.Organization?.Name,
        CreatedAt = user.CreatedAt
    };
}
