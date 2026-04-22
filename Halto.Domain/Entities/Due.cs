using Halto.Domain.Enums;

namespace Halto.Domain.Entities;

public class Due
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public DueStatus Status { get; set; } = DueStatus.Due;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    // Navigation
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
