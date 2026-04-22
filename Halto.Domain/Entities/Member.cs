namespace Halto.Domain.Entities;

public class Member
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }              // e.g. "Student", "Working Professional"
    public string? IdDocumentUrl { get; set; }            // Azure Blob URL for uploaded ID
    public string? IdDocumentType { get; set; }           // "Aadhaar", "Passport", "DL", etc.
    public bool IsActive { get; set; } = true;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DiscontinuedAt { get; set; }
    public string? DiscontinuedReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Business-specific fields stored as JSON (hostel: room/bed, tuition: course/batch, etc.)
    public string? ExtraFieldsJson { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // Category (drives monthly rent, admission fee, deposit)
    public Guid? CategoryId { get; set; }
    public MemberCategory? Category { get; set; }

    // Optional login credentials for member portal
    public Guid? UserId { get; set; }
    public User? User { get; set; }

    // Navigation
    public ICollection<Due> Dues { get; set; } = new List<Due>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
