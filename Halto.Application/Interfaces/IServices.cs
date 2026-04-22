using Halto.Application.Common;
using Halto.Application.DTOs.Auth;
using Halto.Application.DTOs.Categories;
using Halto.Application.DTOs.Dashboard;
using Halto.Application.DTOs.Dues;
using Halto.Application.DTOs.Members;
using Halto.Application.DTOs.Organizations;
using Halto.Application.DTOs.Payments;
using Halto.Application.DTOs.Staff;

namespace Halto.Application.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
    Task<Result<LoginResponse>> RegisterOwnerAsync(RegisterOwnerRequest request);
    Task<Result<UserDto>> GetMeAsync(Guid userId);
}

public interface IOrganizationService
{
    Task<Result<OrganizationDto>> CreateAsync(CreateOrganizationWithOwnerRequest request);
    Task<Result<List<OrganizationDto>>> GetAllAsync();
    Task<Result<OrganizationDto>> GetByIdAsync(Guid id);
    Task<Result<OrganizationDto>> UpdateStatusAsync(Guid id, bool isActive);
    Task<Result<SuperAdminSummaryDto>> GetSuperAdminSummaryAsync();

}

public interface IStaffService
{
    Task<Result<StaffDto>> CreateStaffAsync(Guid organizationId, CreateStaffRequest request);
    Task<Result<List<StaffDto>>> GetStaffAsync(Guid organizationId);
    Task<Result<StaffDto>> UpdateStaffStatusAsync(Guid organizationId, Guid staffId, bool isActive);
}

public interface ICategoryService
{
    Task<Result<CategoryDto>> CreateAsync(Guid organizationId, CreateCategoryRequest request);
    Task<Result<List<CategoryDto>>> GetAllAsync(Guid organizationId);
    Task<Result<CategoryDto>> GetByIdAsync(Guid organizationId, Guid id);
    Task<Result<CategoryDto>> UpdateAsync(Guid organizationId, Guid id, UpdateCategoryRequest request);
    Task<Result<bool>> DeleteAsync(Guid organizationId, Guid id);
}

public interface IMemberService
{
    Task<Result<MemberDto>> CreateMemberAsync(Guid organizationId, CreateMemberRequest request);
    Task<Result<PagedResult<MemberDto>>> GetMembersAsync(Guid organizationId, string? search, int page, int pageSize);
    Task<Result<MemberDto>> GetMemberByIdAsync(Guid organizationId, Guid memberId);
    Task<Result<MemberDto>> UpdateMemberAsync(Guid organizationId, Guid memberId, UpdateMemberRequest request);
    Task<Result<MemberDto>> DiscontinueMemberAsync(Guid organizationId, Guid memberId, string? reason);
    Task<Result<UserDto>> CreateMemberLoginAsync(Guid organizationId, Guid memberId, CreateMemberLoginRequest request);
}

public interface IDueService
{
    Task<Result<GenerateDuesResponse>> GenerateDuesAsync(Guid organizationId, Guid generatedByUserId, GenerateDuesRequest request);
    Task<Result<List<DueDto>>> GetDuesAsync(Guid organizationId, Guid? memberId, int? year, int? month, string? status);
    Task<Result<DueDto>> GetDueByIdAsync(Guid organizationId, Guid dueId);
}

public interface IPaymentService
{
    Task<Result<PaymentDto>> AddPaymentAsync(Guid organizationId, Guid dueId, Guid markedByUserId, CreatePaymentRequest request);
    Task<Result<List<PaymentDto>>> GetPaymentsAsync(Guid organizationId, Guid? memberId, DateTime? from, DateTime? to);
}

public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetSummaryAsync(Guid organizationId, DateTime? from, DateTime? to);
    Task<Result<List<MonthlyChartDto>>> GetMonthlyChartAsync(Guid organizationId, int year);
    Task<Result<PagedResult<MemberDueStatusDto>>> GetMembersDueStatusAsync(Guid organizationId, int year, int month, string? search, int page, int pageSize);
    Task<Result<List<UpcomingDueDto>>> GetUpcomingDuesAsync(Guid organizationId, int daysAhead);
}

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string role, Guid? organizationId);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}

public interface IBlobStorageService
{
    /// <summary>Upload a file stream and return the public URL.</summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string? folder = null);

    /// <summary>Delete a blob by its full URL or blob name.</summary>
    Task DeleteAsync(string blobUrl);

    /// <summary>Generate a short-lived SAS read URL for a private blob.</summary>
    string GetSasUrl(string blobName, TimeSpan expiry);
}
