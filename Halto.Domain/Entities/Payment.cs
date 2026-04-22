using Halto.Domain.Enums;

namespace Halto.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal AmountPaid { get; set; }
    public DateTime PaidOn { get; set; } = DateTime.UtcNow;
    public PaymentMethod Method { get; set; } = PaymentMethod.Manual;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;

    public Guid DueId { get; set; }
    public Due Due { get; set; } = null!;

    public Guid MarkedByUserId { get; set; }
    public User MarkedByUser { get; set; } = null!;
}
