using Halto.Domain.Enums;

namespace Halto.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public BusinessType BusinessType { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Member> Members { get; set; } = new List<Member>();
    public ICollection<Due> Dues { get; set; } = new List<Due>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<MemberCategory> Categories { get; set; } = new List<MemberCategory>();
}
