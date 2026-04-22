using System.Text.Json;

namespace Halto.Application.DTOs.Members;

public class CreateMemberRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? IdDocumentType { get; set; }
    public string? IdDocumentUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? JoinedAt { get; set; }
    public JsonElement? ExtraFields { get; set; }
}

public class UpdateMemberRequest
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? IdDocumentType { get; set; }
    public string? IdDocumentUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public JsonElement? ExtraFields { get; set; }
}

public class DiscontinueMemberRequest
{
    public string? Reason { get; set; }
}

public class MemberDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public string? IdDocumentType { get; set; }
    public string? IdDocumentUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public DateTime? DiscontinuedAt { get; set; }
    public string? DiscontinuedReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? MonthlyRent { get; set; }
    public object? ExtraFields { get; set; }
    public bool HasLogin { get; set; }
    public Guid? UserId { get; set; }
}

public class CreateMemberLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
