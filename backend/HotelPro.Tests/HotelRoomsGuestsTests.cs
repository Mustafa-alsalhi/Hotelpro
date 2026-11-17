using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelPro.Application.Common;
using HotelPro.Application.DTOs.Auth;
using HotelPro.Application.DTOs.Guests;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Tests;

public class HotelRoomsGuestsTests : IAsyncLifetime
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
    public async Task Rooms_Create_RejectsDuplicateRoomNumber_AndBlocksManualOccupiedStatus()
    {
        var rtRes = await _client.PostAsJsonAsync("/api/v1/room-types",
            new SaveRoomTypeRequest("Deluxe", "DLX", "Deluxe Room", 500m, 2, 1, 1, 35m, true, []));
        rtRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var rt = await rtRes.Content.ReadFromJsonAsync<RoomTypeDto>();

        var roomRes = await _client.PostAsJsonAsync("/api/v1/rooms",
            new SaveRoomRequest(rt!.Id, "101", 1, 3, 1, null, "Room 101", null, [], []));
        roomRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var room = await roomRes.Content.ReadFromJsonAsync<RoomDto>();

        // Duplicate room number -> 409 Conflict
        var dupRes = await _client.PostAsJsonAsync("/api/v1/rooms",
            new SaveRoomRequest(rt.Id, "101", 1, 3, 1, null, "Dup", null, [], []));
        dupRes.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Manual change to Maintenance -> 200 OK
        var maintRes = await _client.PatchAsJsonAsync($"/api/v1/rooms/{room!.Id}/status",
            new ChangeRoomStatusRequest(RoomStatus.Maintenance, "AC check"));
        maintRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Manual change to Occupied -> 400 BadRequest (must go through Check-In)
        var occRes = await _client.PatchAsJsonAsync($"/api/v1/rooms/{room.Id}/status",
            new ChangeRoomStatusRequest(RoomStatus.Occupied, null));
        occRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Guests_CreateAndSearchByDocumentNumber_Works()
    {
        var createRes = await _client.PostAsJsonAsync("/api/v1/guests",
            new SaveGuestRequest(
                "Khalid Al-Mansoor",
                "+966555112233",
                "khalid@example.com",
                "Saudi Arabia",
                null,
                "Riyadh",
                "VIP",
                true,
                [new SaveGuestDocumentRequest(DocumentType.Passport, "P99887766", "SA", null)]));

        createRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var searchRes = await _client.GetFromJsonAsync<PagedResult<GuestDto>>("/api/v1/guests?search=P99887766");
        searchRes.Should().NotBeNull();
        searchRes!.Items.Should().ContainSingle(g => g.FullName == "Khalid Al-Mansoor" && g.IsVip);
    }
}

