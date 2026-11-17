using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelPro.Application.Common;
using HotelPro.Application.DTOs.Auth;
using HotelPro.Application.DTOs.Guests;
using HotelPro.Application.DTOs.Operations;
using HotelPro.Application.DTOs.Rates;
using HotelPro.Application.DTOs.Reservations;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Tests;

public class OperationsTests : IAsyncLifetime
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
    public async Task FullStayLifecycle_CheckIn_ServiceOrder_CheckOut_HousekeepingRestore()
    {
        var rtRes = await _client.PostAsJsonAsync("/api/v1/room-types",
            new SaveRoomTypeRequest("Deluxe", "DLX", "Deluxe", 500m, 2, 1, 1, 35m, true, []));
        var rt = await rtRes.Content.ReadFromJsonAsync<RoomTypeDto>();

        var roomRes = await _client.PostAsJsonAsync("/api/v1/rooms",
            new SaveRoomRequest(rt!.Id, "401", 4, 3, 1, null, "401", null, [], []));
        var room = await roomRes.Content.ReadFromJsonAsync<RoomDto>();

        var guestRes = await _client.PostAsJsonAsync("/api/v1/guests",
            new SaveGuestRequest("Nora Al-Saud", "+966501234567", null, "SA", null, null, null, false, []));
        var guest = await guestRes.Content.ReadFromJsonAsync<GuestDto>();

        var rpRes = await _client.PostAsJsonAsync("/api/v1/rate-plans",
            new SaveRatePlanRequest("Standard", "STD", null, true, 1.0m, 7, 0m, null, true));
        var rp = await rpRes.Content.ReadFromJsonAsync<RatePlanDto>();

        var resCreate = await _client.PostAsJsonAsync("/api/v1/reservations",
            new CreateReservationRequest(guest!.Id, rp!.Id, new DateOnly(2026, 11, 20), new DateOnly(2026, 11, 22), 2, 0, BookingSource.DirectBooking, [room!.Id], 0m, 0m, null, true));
        resCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var reservation = await resCreate.Content.ReadFromJsonAsync<ReservationDto>();

        // 1. Check-in
        var checkInRes = await _client.PostAsJsonAsync("/api/v1/front-desk/check-in",
            new CheckInRequest(reservation!.Id, 2, 0, true, "VIP arrival"));
        checkInRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var roomAfterIn = await _client.GetFromJsonAsync<RoomDto>($"/api/v1/rooms/{room.Id}");
        roomAfterIn!.Status.Should().Be(RoomStatus.Occupied);

        // 2. Add guest service order
        var svcRes = await _client.PostAsJsonAsync("/api/v1/guest-services/catalog",
            new SaveGuestServiceRequest("Room Breakfast", null, ServiceCategory.RoomService, 100m, false, true));
        var svc = await svcRes.Content.ReadFromJsonAsync<GuestServiceDto>();

        var orderRes = await _client.PostAsJsonAsync("/api/v1/guest-services/orders",
            new CreateServiceOrderRequest(reservation.Id, room.Id, null, [new CreateServiceOrderItemRequest(svc!.Id, 2)]));
        orderRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Check-out summary includes service order (1000 + 15% tax = 1150 + 200 = 1350)
        var summary = await _client.GetFromJsonAsync<CheckOutSummaryDto>($"/api/v1/front-desk/check-out/{reservation.Id}/summary");
        summary!.ServicesTotal.Should().Be(200m);
        summary.BalanceDue.Should().Be(1350m);

        // 4. Unpaid checkout rejected
        var unpaidOut = await _client.PostAsJsonAsync("/api/v1/front-desk/check-out",
            new CheckOutRequest(reservation.Id, 0m, false, null));
        unpaidOut.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 5. Settled checkout succeeds -> Room becomes Cleaning + Housekeeping task created
        var paidOut = await _client.PostAsJsonAsync("/api/v1/front-desk/check-out",
            new CheckOutRequest(reservation.Id, 1350m, false, "Paid in full"));
        paidOut.StatusCode.Should().Be(HttpStatusCode.OK);

        var roomAfterOut = await _client.GetFromJsonAsync<RoomDto>($"/api/v1/rooms/{room.Id}");
        roomAfterOut!.Status.Should().Be(RoomStatus.Cleaning);

        // 6. Inspect housekeeping task -> Room becomes Available
        var tasks = await _client.GetFromJsonAsync<PagedResult<HousekeepingTaskDto>>("/api/v1/housekeeping");
        var hkTask = tasks!.Items.First(t => t.RoomId == room.Id);

        var inspectRes = await _client.PutAsJsonAsync($"/api/v1/housekeeping/{hkTask.Id}",
            new UpdateHousekeepingTaskRequest(HousekeepingStatus.Inspected, null, false, "Clean & ready"));
        inspectRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var roomFinal = await _client.GetFromJsonAsync<RoomDto>($"/api/v1/rooms/{room.Id}");
        roomFinal!.Status.Should().Be(RoomStatus.Available);
    }
}
