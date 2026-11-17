using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelPro.Application.DTOs.Auth;

namespace HotelPro.Tests;

public class SecurityIntegrationTests : IAsyncLifetime
{
    private readonly HotelProWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsJwtAndRefreshToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin@test.local", "Admin@12345"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        auth.Should().NotBeNull();
        auth!.AccessToken.Should().NotBeNullOrWhiteSpace();
        auth.RefreshToken.Should().NotBeNullOrWhiteSpace();
        auth.User.Roles.Should().Contain("SuperAdmin");
        auth.User.Permissions.Should().Contain("Users.View");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin@test.local", "WrongPassword123"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_RotatesToken_AndRejectsReuse()
    {
        var loginRes = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin@test.local", "Admin@12345"));
        var auth = await loginRes.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest(auth!.RefreshToken));
        refreshRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await refreshRes.Content.ReadFromJsonAsync<AuthResponse>();
        rotated!.RefreshToken.Should().NotBe(auth.RefreshToken);

        // Reuse of old refresh token must be rejected
        var reuseRes = await _client.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest(auth.RefreshToken));
        reuseRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UsersEndpoint_EnforcesPermissions()
    {
        // Unauthenticated -> 401
        var unauthRes = await _client.GetAsync("/api/v1/users");
        unauthRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Receptionist without Users.View -> 403
        var recLogin = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("reception@test.local", "Reception@12345"));
        var recAuth = await recLogin.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", recAuth!.AccessToken);
        var forbiddenRes = await _client.GetAsync("/api/v1/users");
        forbiddenRes.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // SuperAdmin with Users.View -> 200
        var adminLogin = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("admin@test.local", "Admin@12345"));
        var adminAuth = await adminLogin.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth!.AccessToken);
        var okRes = await _client.GetAsync("/api/v1/users");
        okRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

