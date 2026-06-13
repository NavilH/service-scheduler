using System.Net;
using System.Net.Http.Json;
using ServiceScheduler.Shared.DTOs;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Tests;

public class ServicesControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ServicesControllerTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveServices_ByDefault()
    {
        using var db = _factory.CreateDbContext();
        db.ServiceItems.AddRange(
            new ServiceItem { Name = "Active Service", DurationMinutes = 30, Price = 50, IsActive = true },
            new ServiceItem { Name = "Inactive Service", DurationMinutes = 30, Price = 50, IsActive = false });
        await db.SaveChangesAsync();

        var response = await _factory.CreateClient().GetAsync("/api/services");
        var items = await response.Content.ReadFromJsonAsync<List<ServiceItemDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(items!, i => Assert.True(i.IsActive));
    }

    [Fact]
    public async Task GetAll_ReturnsForbidden_WhenNonAdminRequestsInactive()
    {
        var response = await _factory.CreateClient().GetAsync("/api/services?includeInactive=true");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_IncludesInactiveServices_WhenAdmin()
    {
        using var db = _factory.CreateDbContext();
        db.ServiceItems.Add(new ServiceItem { Name = "Hidden Service", DurationMinutes = 30, Price = 10, IsActive = false });
        await db.SaveChangesAsync();

        var response = await _factory.CreateAdminClient().GetAsync("/api/services?includeInactive=true");
        var items = await response.Content.ReadFromJsonAsync<List<ServiceItemDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(items!, i => !i.IsActive);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenServiceDoesNotExist()
    {
        var response = await _factory.CreateClient().GetAsync("/api/services/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsCreated_WithCorrectData_WhenAdmin()
    {
        var dto = new CreateServiceItemDto { Name = "New Service", DurationMinutes = 60, Price = 100 };

        var response = await _factory.CreateAdminClient().PostAsJsonAsync("/api/services", dto);
        var created = await response.Content.ReadFromJsonAsync<ServiceItemDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("New Service", created!.Name);
        Assert.Equal(60, created.DurationMinutes);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var dto = new CreateServiceItemDto { Name = "Unauthorized", DurationMinutes = 30, Price = 50 };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/services", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletes_WhenNoActiveAppointments()
    {
        using var db = _factory.CreateDbContext();
        var service = new ServiceItem { Name = "Delete Me", DurationMinutes = 30, Price = 20, IsActive = true };
        db.ServiceItems.Add(service);
        await db.SaveChangesAsync();

        var response = await _factory.CreateAdminClient().DeleteAsync($"/api/services/{service.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var verifyDb = _factory.CreateDbContext();
        var updated = await verifyDb.ServiceItems.FindAsync(service.Id);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task Update_ReturnsNoContent_WhenAdmin()
    {
        using var db = _factory.CreateDbContext();
        var service = new ServiceItem { Name = "Update Me", DurationMinutes = 30, Price = 40, IsActive = true };
        db.ServiceItems.Add(service);
        await db.SaveChangesAsync();

        var dto = new UpdateServiceItemDto { Name = "Updated Name", DurationMinutes = 45, Price = 55, IsActive = true };
        var response = await _factory.CreateAdminClient().PutAsJsonAsync($"/api/services/{service.Id}", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var verifyDb = _factory.CreateDbContext();
        var updated = await verifyDb.ServiceItems.FindAsync(service.Id);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal(45, updated.DurationMinutes);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDurationIsOutOfRange()
    {
        var dto = new CreateServiceItemDto { Name = "Bad Duration", DurationMinutes = 0, Price = 50 };

        var response = await _factory.CreateAdminClient().PostAsJsonAsync("/api/services", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsConflict_WhenActiveAppointmentsExist()
    {
        using var db = _factory.CreateDbContext();
        var service = new ServiceItem { Name = "Busy Service", DurationMinutes = 30, Price = 20, IsActive = true };
        db.ServiceItems.Add(service);
        await db.SaveChangesAsync();

        db.Appointments.Add(new Appointment
        {
            ServiceItemId = service.Id,
            CustomerName = "Test Customer",
            CustomerEmail = "customer@test.com",
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddMinutes(30),
            Status = AppointmentStatus.Pending
        });
        await db.SaveChangesAsync();

        var response = await _factory.CreateAdminClient().DeleteAsync($"/api/services/{service.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
