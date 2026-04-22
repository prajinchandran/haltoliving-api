using Halto.Domain.Enums;

namespace Halto.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Null for SuperAdmin
    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    // If this user is a Member, link to Member record
    public Member? Member { get; set; }
}
