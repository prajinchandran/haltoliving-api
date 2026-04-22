using Halto.Domain.Enums;

namespace Halto.Application.DTOs.Organizations;

public class CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public BusinessType BusinessType { get; set; }
}

public class CreateOrganizationWithOwnerRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public BusinessType BusinessType { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public string OwnerPassword { get; set; } = string.Empty;
    public string OwnerFullName { get; set; } = string.Empty;
    public string? OwnerPhone { get; set; }
}

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }

    // Revenue stats
    public decimal TotalRevenue { get; set; }
    public decimal TotalDue { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int DueCount { get; set; }
    public int PaidCount { get; set; }
    public int PartialCount { get; set; }
}

public class UpdateOrganizationStatusRequest
{
    public bool IsActive { get; set; }
}

public class SuperAdminSummaryDto
{
    public int TotalOrganizations { get; set; }
    public int ActiveOrganizations { get; set; }
    public int TotalMembers { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<OrgRevenueDto> TopOrganizations { get; set; } = new();
}

public class OrgRevenueDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int MemberCount { get; set; }
}
