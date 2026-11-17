using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HotelPro.Application.Common;
using HotelPro.Application.DTOs.Auth;
using HotelPro.Application.DTOs.HR;
using HotelPro.Application.DTOs.Inventory;
using HotelPro.Domain.Enums;

namespace HotelPro.Tests;

public class HrAndInventoryTests : IAsyncLifetime
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
    public async Task Inventory_PurchaseOrderReceive_And_InsufficientStockGuard()
    {
        var catRes = await _client.PostAsJsonAsync("/api/v1/inventory/categories", "Cleaning");
        var cat = await catRes.Content.ReadFromJsonAsync<InventoryCategoryDto>();

        var itemRes = await _client.PostAsJsonAsync("/api/v1/inventory/items",
            new SaveInventoryItemRequest("CLN-001", "Floor Detergent", cat!.Id, "Bottle", 5m, 15m, true));
        itemRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var item = await itemRes.Content.ReadFromJsonAsync<InventoryItemDto>();
        item!.CurrentStock.Should().Be(0m);
        item.IsLowStock.Should().BeTrue();

        var supRes = await _client.PostAsJsonAsync("/api/v1/inventory/suppliers",
            new SaveSupplierRequest("CleanCo", "Ali", "+966500001111", null, null, true));
        var sup = await supRes.Content.ReadFromJsonAsync<SupplierDto>();

        // Create & Receive PO for 20 bottles
        var poRes = await _client.PostAsJsonAsync("/api/v1/inventory/purchase-orders",
            new CreatePurchaseOrderRequest(sup!.Id, null, [new PurchaseOrderItemRequest(item.Id, 20m, 15m)]));
        var po = await poRes.Content.ReadFromJsonAsync<PurchaseOrderDto>();

        var recvRes = await _client.PostAsJsonAsync($"/api/v1/inventory/purchase-orders/{po!.Id}/receive", new { });
        recvRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Issue 25 bottles (> 20 available) -> 400 BadRequest
        var badIssue = await _client.PostAsJsonAsync("/api/v1/inventory/transactions",
            new CreateStockTransactionRequest(item.Id, StockTransactionType.DepartmentIssue, 25m, null, null, "Too much"));
        badIssue.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Issue 10 bottles -> BalanceAfter = 10
        var okIssue = await _client.PostAsJsonAsync("/api/v1/inventory/transactions",
            new CreateStockTransactionRequest(item.Id, StockTransactionType.DepartmentIssue, 10m, null, null, "HK issue"));
        okIssue.StatusCode.Should().Be(HttpStatusCode.OK);
        var tx = await okIssue.Content.ReadFromJsonAsync<StockTransactionDto>();
        tx!.BalanceAfter.Should().Be(10m);
    }

    [Fact]
    public async Task Employees_CreateAndAttendance_Works()
    {
        var deptRes = await _client.PostAsJsonAsync("/api/v1/employees/departments",
            new SaveDepartmentRequest("Reception", "REC", "Front Desk"));
        var dept = await deptRes.Content.ReadFromJsonAsync<DepartmentDto>();

        var empRes = await _client.PostAsJsonAsync("/api/v1/employees",
            new SaveEmployeeRequest("EMP-001", "Sami Al-Qahtani", "+966500112233", "sami@hotel.local", "Receptionist", dept!.Id, new DateOnly(2026, 1, 1), 6000m, EmployeeStatus.Active, null, null, null));
        empRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var emp = await empRes.Content.ReadFromJsonAsync<EmployeeDto>();

        var attRes = await _client.PostAsJsonAsync("/api/v1/employees/attendance",
            new RecordAttendanceRequest(emp!.Id, new DateOnly(2026, 11, 17), new TimeOnly(8, 0), null, AttendanceStatus.Present, "On time"));
        attRes.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

