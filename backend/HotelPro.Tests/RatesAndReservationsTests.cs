using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelPro.Application.DTOs.Auth;
using HotelPro.Application.DTOs.Guests;
using HotelPro.Application.DTOs.Rates;
using HotelPro.Application.DTOs.Reservations;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Tests;

public class RatesAndReservationsTests : IAsyncLifetime
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
    public async Task Reservation_PreventsOverlappingDates_AndFreesRoomOnCancel()
    {
        var rtRes = await _client.PostAsJsonAsync("/api/v1/room-types",
            new SaveRoomTypeRequest("Suite", "STE", "Suite", 1000m, 2, 1, 1, 55m, true, []));
        var rt = await rtRes.Content.ReadFromJsonAsync<RoomTypeDto>();

        var roomRes = await _client.PostAsJsonAsync("/api/v1/rooms",
            new SaveRoomRequest(rt!.Id, "301", 3, 3, 1, null, "Suite 301", null, [], []));
        var room = await roomRes.Content.ReadFromJsonAsync<RoomDto>();

        var guestRes = await _client.PostAsJsonAsync("/api/v1/guests",
            new SaveGuestRequest("Omar Tariq", "+966500001122", null, "SA", null, null, null, false, []));
        var guest = await guestRes.Content.ReadFromJsonAsync<GuestDto>();

        var rpRes = await _client.PostAsJsonAsync("/api/v1/rate-plans",
            new SaveRatePlanRequest("Standard", "STD", null, true, 1.0m, 7, 10m, null, true));
        var rp = await rpRes.Content.ReadFromJsonAsync<RatePlanDto>();

        var checkIn = new DateOnly(2026, 12, 10);
        var checkOut = new DateOnly(2026, 12, 13);

        // 1. First booking succeeds
        var book1 = await _client.PostAsJsonAsync("/api/v1/reservations",
            new CreateReservationRequest(guest!.Id, rp!.Id, checkIn, checkOut, 2, 0, BookingSource.DirectBooking, [room!.Id], 0m, 0m, null, true));
        book1.StatusCode.Should().Be(HttpStatusCode.Created);
        var res1 = await book1.Content.ReadFromJsonAsync<ReservationDto>();
        res1!.Nights.Should().Be(3);

        // 2. Overlapping booking fails with 409 Conflict
        var overlap = await _client.PostAsJsonAsync("/api/v1/reservations",
            new CreateReservationRequest(guest.Id, rp.Id, new DateOnly(2026, 12, 11), new DateOnly(2026, 12, 14), 1, 0, BookingSource.Website, [room.Id], 0m, 0m, null, true));
        overlap.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // 3. Adjacent booking (Dec 13 -> Dec 15) succeeds
        var adjacent = await _client.PostAsJsonAsync("/api/v1/reservations",
            new CreateReservationRequest(guest.Id, rp.Id, new DateOnly(2026, 12, 13), new DateOnly(2026, 12, 15), 1, 0, BookingSource.Phone, [room.Id], 0m, 0m, null, true));
        adjacent.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Cancel first booking -> Dec 10-13 becomes available again
        var cancelRes = await _client.PostAsJsonAsync($"/api/v1/reservations/{res1.Id}/cancel",
            new CancelReservationRequest("Guest changed plans"));
        cancelRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var rebook = await _client.PostAsJsonAsync("/api/v1/reservations",
            new CreateReservationRequest(guest.Id, rp.Id, checkIn, checkOut, 1, 0, BookingSource.DirectBooking, [room.Id], 0m, 0m, null, true));
        rebook.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}

