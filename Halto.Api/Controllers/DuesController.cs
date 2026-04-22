using Halto.Application.DTOs.Dues;
using Halto.Application.DTOs.Payments;
using Halto.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Halto.Api.Controllers;

[Route("api/dues")]
[Authorize(Roles = "OrganizationOwner,OrganizationStaff")]
public class DuesController : HaltoControllerBase
{
    private readonly IDueService _dueService;
    private readonly IPaymentService _paymentService;

    public DuesController(IDueService dueService, IPaymentService paymentService)
    {
        _dueService = dueService;
        _paymentService = paymentService;
    }

    /// <summary>
    /// Generate monthly dues for all active members or a single member.
    /// Skips if due already exists for the month.
    /// Amount resolved from: AmountOverride > member.ExtraFieldsJson (monthlyRent/monthlyFee/etc)
    /// </summary>
    [HttpPost("generate-month")]
    public async Task<IActionResult> GenerateDues([FromBody] GenerateDuesRequest request)
    {
        var result = await _dueService.GenerateDuesAsync(CurrentOrgId, CurrentUserId, request);
        return ToActionResult(result);
    }

    /// <summary>Query dues with optional filters</summary>
    [HttpGet]
    public async Task<IActionResult> GetDues(
        [FromQuery] Guid? memberId,
        [FromQuery] int? year,
        [FromQuery] int? month,
        [FromQuery] string? status)
    {
        var result = await _dueService.GetDuesAsync(CurrentOrgId, memberId, year, month, status);
        return ToActionResult(result);
    }

    /// <summary>Get a specific due with all payment history</summary>
    [HttpGet("{dueId:guid}")]
    public async Task<IActionResult> GetDue(Guid dueId)
    {
        var result = await _dueService.GetDueByIdAsync(CurrentOrgId, dueId);
        return ToActionResult(result);
    }

    /// <summary>
    /// Record a payment against a due. Supports partial payments.
    /// Due status auto-updates to Partial or Paid based on cumulative payments.
    /// </summary>
    [HttpPost("{dueId:guid}/payments")]
    public async Task<IActionResult> AddPayment(Guid dueId, [FromBody] CreatePaymentRequest request)
    {
        var result = await _paymentService.AddPaymentAsync(CurrentOrgId, dueId, CurrentUserId, request);
        return ToActionResult(result);
    }
}

[Route("api/payments")]
[Authorize(Roles = "OrganizationOwner,OrganizationStaff")]
public class PaymentsController : HaltoControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Query payment history with optional filters</summary>
    [HttpGet]
    public async Task<IActionResult> GetPayments(
        [FromQuery] Guid? memberId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var result = await _paymentService.GetPaymentsAsync(CurrentOrgId, memberId, from, to);
        return ToActionResult(result);
    }
}
