using Halto.Domain.Enums;

namespace Halto.Application.DTOs.Dues;

public class GenerateDuesRequest
{
    public int Year { get; set; }
    public int Month { get; set; }
    // If null, generate for ALL active members
    public Guid? MemberId { get; set; }
    // If null, use amount from member's ExtraFieldsJson (e.g. monthlyRent), or provide override
    public decimal? AmountOverride { get; set; }
    public string? Notes { get; set; }
}

public class DueDto
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Amount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PaymentSummaryDto> Payments { get; set; } = new();
}

public class GenerateDuesResponse
{
    public int Generated { get; set; }
    public int Skipped { get; set; }
    public List<string> SkippedReasons { get; set; } = new();
}

// Summary used inside DueDto — lives here since it's part of a due's detail view
public class PaymentSummaryDto
{
    public Guid Id { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaidOn { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
