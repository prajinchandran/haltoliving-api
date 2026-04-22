namespace Halto.Domain.Entities;

public class MemberCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;         // e.g. "4 Share with Food"
    public string? Description { get; set; }
    public decimal MonthlyRent { get; set; }                  // monthly due amount
    public decimal AdmissionFee { get; set; }                 // non-refundable
    public decimal DepositAmount { get; set; }                // refundable deposit
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    // Navigation
    public ICollection<Member> Members { get; set; } = new List<Member>();
}
