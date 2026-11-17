using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelPro.Application.DTOs.Auth;
using HotelPro.Application.DTOs.Dashboard;
using HotelPro.Application.DTOs.Reports;
using HotelPro.Application.DTOs.Rooms;
using HotelPro.Domain.Enums;

namespace HotelPro.Tests;

public class DashboardAndReportsTests : IAsyncLifetime
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
    public async Task Dashboard_And_OccupancyReport_ExcludeOutOfServiceRoomsFromDenominator()
    {
        var rtRes = await _client.PostAsJsonAsync("/api/v1/room-types",
            new SaveRoomTypeRequest("Single", "SGL", "Single", 300m, 1, 0, 1, 20m, true, []));
        var rt = await rtRes.Content.ReadFromJsonAsync<RoomTypeDto>();

        var r1 = await (await _client.PostAsJsonAsync("/api/v1/rooms",
            new SaveRoomRequest(rt!.Id, "601", 6, 1, 1, null, "601", null, [], []))).Content.ReadFromJsonAsync<RoomDto>();
        var r2 = await (await _client.PostAsJsonAsync("/api/v1/rooms",
            new SaveRoomRequest(rt.Id, "602", 6, 1, 1, null, "602", null, [], []))).Content.ReadFromJsonAsync<RoomDto>();

        // Put r2 OutOfService -> serviceable = 1
        await _client.PatchAsJsonAsync($"/api/v1/rooms/{r2!.Id}/status",
            new ChangeRoomStatusRequest(RoomStatus.OutOfService, "Renovation"));

        var dash = await _client.GetFromJsonAsync<DashboardSummaryDto>("/api/v1/dashboard/summary");
        dash!.TotalRooms.Should().Be(2);
        dash.OutOfServiceRooms.Should().Be(1);
        dash.AvailableRooms.Should().Be(1);

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("O");
        var occ = await _client.GetFromJsonAsync<OccupancyReportDto>($"/api/v1/reports/occupancy?from={today}&to={today}");
        occ!.Rows.Should().ContainSingle(x => x.TotalServiceableRooms == 1 && x.OutOfServiceRooms == 1);

        var csvRes = await _client.GetAsync($"/api/v1/reports/export/occupancy-csv?from={today}&to={today}");
        csvRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

