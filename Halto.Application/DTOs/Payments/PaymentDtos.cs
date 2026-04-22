using Halto.Domain.Enums;

namespace Halto.Application.DTOs.Payments;

public class CreatePaymentRequest
{
    public decimal AmountPaid { get; set; }
    public DateTime? PaidOn { get; set; }
    public string? Notes { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Manual;
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid DueId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int DueYear { get; set; }
    public int DueMonth { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaidOn { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string MarkedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
