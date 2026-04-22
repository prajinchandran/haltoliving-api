using Halto.Application.Common;
using Halto.Application.DTOs.Payments;
using Halto.Application.Interfaces;
using Halto.Domain.Entities;
using Halto.Domain.Enums;
using Halto.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Halto.Infrastructure.Services;

public class PaymentService : IPaymentService
{
    private readonly HaltoDbContext _db;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(HaltoDbContext db, ILogger<PaymentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<PaymentDto>> AddPaymentAsync(Guid organizationId, Guid dueId, Guid markedByUserId, CreatePaymentRequest request)
    {
        if (request.AmountPaid <= 0)
            return Result<PaymentDto>.Failure("Payment amount must be greater than zero.");

        var due = await _db.Dues
            .Include(d => d.Payments)
            .Include(d => d.Member)
            .FirstOrDefaultAsync(d => d.Id == dueId && d.OrganizationId == organizationId);

        if (due is null)
            return Result<PaymentDto>.NotFound("Due not found.");

        if (due.Status == DueStatus.Paid)
            return Result<PaymentDto>.Failure("This due is already fully paid.");

        var currentPaid = due.Payments.Sum(p => p.AmountPaid);
        var remaining = due.Amount - currentPaid;

        if (request.AmountPaid > remaining)
            return Result<PaymentDto>.Failure($"Payment amount ({request.AmountPaid:C}) exceeds remaining balance ({remaining:C}).");

        var payment = new Payment
        {
            OrganizationId = organizationId,
            MemberId = due.MemberId,
            DueId = due.Id,
            AmountPaid = request.AmountPaid,
            PaidOn = request.PaidOn?.ToUniversalTime() ?? DateTime.UtcNow,
            Method = request.Method,
            Notes = request.Notes,
            MarkedByUserId = markedByUserId
        };
        _db.Payments.Add(payment);

        // Update due status based on total payments
        var newTotal = currentPaid + request.AmountPaid;
        due.Status = newTotal >= due.Amount ? DueStatus.Paid : DueStatus.Partial;
        due.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Payment of {Amount} recorded for due {DueId}", request.AmountPaid, dueId);

        var markedBy = await _db.Users.FindAsync(markedByUserId);
        return Result<PaymentDto>.Success(new PaymentDto
        {
            Id = payment.Id,
            DueId = payment.DueId,
            MemberId = payment.MemberId,
            MemberName = due.Member.FullName,
            DueYear = due.Year,
            DueMonth = due.Month,
            AmountPaid = payment.AmountPaid,
            PaidOn = payment.PaidOn,
            Method = payment.Method.ToString(),
            Notes = payment.Notes,
            MarkedBy = markedBy?.FullName ?? string.Empty,
            CreatedAt = payment.CreatedAt
        }, 201);
    }

    public async Task<Result<List<PaymentDto>>> GetPaymentsAsync(Guid organizationId, Guid? memberId, DateTime? from, DateTime? to)
    {
        var query = _db.Payments
            .Include(p => p.Member)
            .Include(p => p.MarkedByUser)
            .Include(p => p.Due)
            .Where(p => p.OrganizationId == organizationId);

        if (memberId.HasValue) query = query.Where(p => p.MemberId == memberId.Value);
        if (from.HasValue) query = query.Where(p => p.PaidOn >= from.Value.ToUniversalTime());
        if (to.HasValue) query = query.Where(p => p.PaidOn <= to.Value.ToUniversalTime().AddDays(1));

        var payments = await query.OrderByDescending(p => p.PaidOn).ToListAsync();

        return Result<List<PaymentDto>>.Success(payments.Select(p => new PaymentDto
        {
            Id = p.Id,
            DueId = p.DueId,
            MemberId = p.MemberId,
            MemberName = p.Member.FullName,
            DueYear = p.Due.Year,
            DueMonth = p.Due.Month,
            AmountPaid = p.AmountPaid,
            PaidOn = p.PaidOn,
            Method = p.Method.ToString(),
            Notes = p.Notes,
            MarkedBy = p.MarkedByUser.FullName,
            CreatedAt = p.CreatedAt
        }).ToList());
    }
}
