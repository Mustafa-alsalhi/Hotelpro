using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelPro.Application.DTOs.Auth;
using HotelPro.Application.DTOs.Finance;
using HotelPro.Application.DTOs.Guests;
using HotelPro.Application.DTOs.Rates;
using HotelPro.Application.DTOs.Reservations;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Tests;

public class BillingFinanceTests : IAsyncLifetime
{
    private readonly HotelProWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();

        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin@test.local", "Admin@12345"));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Billing_PartialPayment_Idempotency_Reversal_And_FinanceSummary()
    {
        var rtRes = await _client.PostAsJsonAsync("/api/v1/room-types",
            new SaveRoomTypeRequest("Standard", "STD", "Standard", 500m, 2, 0, 1, 25m, true, []));
        var rt = await rtRes.Content.ReadFromJsonAsync<RoomTypeDto>();

        var roomRes = await _client.PostAsJsonAsync("/api/v1/rooms",
            new SaveRoomRequest(rt!.Id, "501", 5, 2, 1, null, "501", null, [], []));
        var room = await roomRes.Content.ReadFromJsonAsync<RoomDto>();

        var guestRes = await _client.PostAsJsonAsync("/api/v1/guests",
            new SaveGuestRequest("Faisal Al-Harbi", "+966509998877", null, "SA", null, null, null, false, []));
        var guest = await guestRes.Content.ReadFromJsonAsync<GuestDto>();

        var rpRes = await _client.PostAsJsonAsync("/api/v1/rate-plans",
            new SaveRatePlanRequest("Standard", "STD", null, true, 1.0m, 7, 0m, null, true));
        var rp = await rpRes.Content.ReadFromJsonAsync<RatePlanDto>();

        // 2 nights * 500 + 15% tax = 1150
        var resCreate = await _client.PostAsJsonAsync("/api/v1/reservations",
            new CreateReservationRequest(guest!.Id, rp!.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), 2, 0, BookingSource.DirectBooking, [room!.Id], 0m, 0m, null, true));
        var reservation = await resCreate.Content.ReadFromJsonAsync<ReservationDto>();

        // 1. Issue Invoice
        var invRes = await _client.PostAsJsonAsync("/api/v1/billing/invoices/issue", new IssueInvoiceRequest(reservation!.Id, null));
        invRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var invoice = await invRes.Content.ReadFromJsonAsync<InvoiceDto>();
        invoice!.GrandTotal.Should().Be(1150m);
        invoice.BalanceDue.Should().Be(1150m);

        // 2. Partial payment (500) with idempotency key
        var idemKey = Guid.NewGuid().ToString();
        var pay1Res = await _client.PostAsJsonAsync("/api/v1/billing/payments",
            new RecordPaymentRequest(invoice.Id, 500m, PaymentMethod.Card, "TXN-1", idemKey, "Deposit"));
        pay1Res.StatusCode.Should().Be(HttpStatusCode.OK);
        var pay1 = await pay1Res.Content.ReadFromJsonAsync<PaymentDto>();

        // Retry with same idempotency key -> returns same receipt, no double charge
        var payRetryRes = await _client.PostAsJsonAsync("/api/v1/billing/payments",
            new RecordPaymentRequest(invoice.Id, 500m, PaymentMethod.Card, "TXN-1", idemKey, "Deposit"));
        var payRetry = await payRetryRes.Content.ReadFromJsonAsync<PaymentDto>();
        payRetry!.Id.Should().Be(pay1!.Id);

        var invAfterPartial = await _client.GetFromJsonAsync<InvoiceDto>($"/api/v1/billing/invoices/{invoice.Id}");
        invAfterPartial!.PaidAmount.Should().Be(500m);
        invAfterPartial.BalanceDue.Should().Be(650m);
        invAfterPartial.Status.Should().Be(InvoiceStatus.PartiallyPaid);

        // 3. Overpayment rejected
        var overPayRes = await _client.PostAsJsonAsync("/api/v1/billing/payments",
            new RecordPaymentRequest(invoice.Id, 900m, PaymentMethod.Cash, null, Guid.NewGuid().ToString(), null));
        overPayRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 4. Reverse payment -> balance restored to 1150
        var revRes = await _client.PostAsJsonAsync($"/api/v1/billing/payments/{pay1.Id}/reverse",
            new ReversePaymentRequest("Card declined"));
        revRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var invAfterRev = await _client.GetFromJsonAsync<InvoiceDto>($"/api/v1/billing/invoices/{invoice.Id}");
        invAfterRev!.PaidAmount.Should().Be(0m);
        invAfterRev.BalanceDue.Should().Be(1150m);

        // 5. Full payment + Expense + Financial Summary
        var fullPayRes = await _client.PostAsJsonAsync("/api/v1/billing/payments",
            new RecordPaymentRequest(invoice.Id, 1150m, PaymentMethod.BankTransfer, "WIRE-99", Guid.NewGuid().ToString(), null));
        fullPayRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var catRes = await _client.PostAsJsonAsync("/api/v1/finance/expense-categories",
            new CreateExpenseCategoryRequest("Utilities", "Electric"));
        var cat = await catRes.Content.ReadFromJsonAsync<ExpenseCategoryDto>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await _client.PostAsJsonAsync("/api/v1/finance/expenses",
            new CreateExpenseRequest(cat!.Id, "Electricity bill", 350m, today, PaymentMethod.BankTransfer, null));

        var revs = await _client.GetFromJsonAsync<HotelPro.Application.Common.PagedResult<RevenueRecordDto>>("/api/v1/finance/revenues");
        revs!.Items.Should().HaveCount(3);

        var isoToday = today.ToString("O");
        var summary = await _client.GetFromJsonAsync<FinancialSummaryDto>($"/api/v1/finance/summary?from={isoToday}&to={isoToday}");
        summary!.RecognizedRevenuesTotal.Should().Be(1150m);
        summary.TotalExpenses.Should().Be(350m);
        summary.NetOperatingResult.Should().Be(800m);
    }
}

