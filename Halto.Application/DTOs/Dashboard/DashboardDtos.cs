namespace Halto.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public decimal TotalDueAmount { get; set; }
    public decimal TotalPaidAmount { get; set; }
    public decimal TotalUnpaidAmount { get; set; }
    public int MemberCount { get; set; }
    public int DueCount { get; set; }
    public int PaidCount { get; set; }
    public int PartialCount { get; set; }
    public int UnpaidCount { get; set; }
}

public class MonthlyChartDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalDue { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public int DueCount { get; set; }
    public int PaidCount { get; set; }
}

public class MemberDueStatusDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public decimal DueAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastPaidDate { get; set; }
}

public class UpcomingDueDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? CategoryName { get; set; }
    public decimal Amount { get; set; }
    public int DueYear { get; set; }
    public int DueMonth { get; set; }
    public string DueMonthName { get; set; } = string.Empty;
    // null = not yet generated, non-null = already has a due record
    public Guid? DueId { get; set; }
    public string? Status { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
}
