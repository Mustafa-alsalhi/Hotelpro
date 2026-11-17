using FluentValidation;
using HotelPro.API.Authorization;
using HotelPro.Application.Common;
using HotelPro.Application.Common.Interfaces;
using HotelPro.Application.DTOs.Finance;
using HotelPro.Domain.Constants;
using HotelPro.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HotelPro.API.Controllers;

[ApiController]
[Route("api/v1/billing")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _service;

    public BillingController(IBillingService service) => _service = service;

    [HttpGet("invoices")]
    [HasPermission(PermissionsCatalog.Billing.View)]
    public async Task<ActionResult<PagedResult<InvoiceDto>>> GetInvoices(
        [FromQuery] PaginationParams pagination,
        [FromQuery] InvoiceStatus? status,
        CancellationToken ct)
        => Ok(await _service.GetInvoicesAsync(pagination, status, ct));

    [HttpGet("invoices/{id:guid}")]
    [HasPermission(PermissionsCatalog.Billing.View)]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(Guid id, CancellationToken ct)
        => Ok(await _service.GetInvoiceByIdAsync(id, ct));

    [HttpPost("invoices/issue")]
    [HasPermission(PermissionsCatalog.Billing.Create)]
    public async Task<ActionResult<InvoiceDto>> IssueInvoice([FromBody] IssueInvoiceRequest request, CancellationToken ct)
        => Ok(await _service.IssueInvoiceForReservationAsync(request, ct));

    [HttpPost("invoices/{id:guid}/cancel")]
    [HasPermission(PermissionsCatalog.Billing.Cancel)]
    public async Task<ActionResult<InvoiceDto>> CancelInvoice(Guid id, [FromBody] CancelInvoiceRequest request, CancellationToken ct)
        => Ok(await _service.CancelInvoiceAsync(id, request, ct));

    [HttpGet("payments")]
    [HasPermission(PermissionsCatalog.Billing.View)]
    public async Task<ActionResult<PagedResult<PaymentDto>>> GetPayments(
        [FromQuery] PaginationParams pagination,
        [FromQuery] Guid? invoiceId,
        CancellationToken ct)
        => Ok(await _service.GetPaymentsAsync(pagination, invoiceId, ct));

    [HttpPost("payments")]
    [HasPermission(PermissionsCatalog.Billing.ManagePayments)]
    public async Task<ActionResult<PaymentDto>> RecordPayment(
        [FromBody] RecordPaymentRequest request,
        [FromServices] IValidator<RecordPaymentRequest> validator,
        CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        return Ok(await _service.RecordPaymentAsync(request, ct));
    }

    [HttpPost("payments/{id:guid}/reverse")]
    [HasPermission(PermissionsCatalog.Billing.Cancel)]
    public async Task<ActionResult<PaymentDto>> ReversePayment(Guid id, [FromBody] ReversePaymentRequest request, CancellationToken ct)
        => Ok(await _service.ReversePaymentAsync(id, request, ct));
}

