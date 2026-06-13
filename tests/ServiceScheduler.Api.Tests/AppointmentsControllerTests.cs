using System.Net;
using System.Net.Http.Json;
using ServiceScheduler.Shared.DTOs;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Tests;

public class AppointmentsControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AppointmentsControllerTests(ApiFactory factory) => _factory = factory;

    private async Task<ServiceItem> SeedServiceAsync()
    {
        using var db = _factory.CreateDbContext();
        var service = new ServiceItem { Name = "Test Service", DurationMinutes = 60, Price = 80, IsActive = true };
        db.ServiceItems.Add(service);
        await db.SaveChangesAsync();
        return service;
    }

    [Fact]
    public async Task Book_ReturnsCreated_ForValidSlot()
    {
        var service = await SeedServiceAsync();
        var dto = new CreateAppointmentDto
        {
            ServiceItemId = service.Id,
            CustomerName = "Jane Doe",
            CustomerEmail = "jane@example.com",
            StartTime = new DateTime(2025, 12, 15, 10, 0, 0, DateTimeKind.Utc)
        };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/appointments", dto);
        var created = await response.Content.ReadFromJsonAsync<AppointmentDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("Jane Doe", created!.CustomerName);
        Assert.Equal(AppointmentStatus.Pending, created.Status);
        Assert.Equal(dto.StartTime.AddMinutes(service.DurationMinutes), created.EndTime);
    }

    [Fact]
    public async Task Book_ReturnsConflict_WhenSlotAlreadyTaken()
    {
        var service = await SeedServiceAsync();
        var startTime = new DateTime(2025, 12, 16, 14, 0, 0, DateTimeKind.Utc);

        using var db = _factory.CreateDbContext();
        db.Appointments.Add(new Appointment
        {
            ServiceItemId = service.Id,
            CustomerName = "First Customer",
            CustomerEmail = "first@example.com",
            StartTime = startTime,
            EndTime = startTime.AddMinutes(service.DurationMinutes),
            Status = AppointmentStatus.Confirmed
        });
        await db.SaveChangesAsync();

        var dto = new CreateAppointmentDto
        {
            ServiceItemId = service.Id,
            CustomerName = "Second Customer",
            CustomerEmail = "second@example.com",
            StartTime = startTime
        };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/appointments", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Book_ReturnsBadRequest_WhenServiceIsInactive()
    {
        using var db = _factory.CreateDbContext();
        var service = new ServiceItem { Name = "Inactive Svc", DurationMinutes = 30, Price = 40, IsActive = false };
        db.ServiceItems.Add(service);
        await db.SaveChangesAsync();

        var dto = new CreateAppointmentDto
        {
            ServiceItemId = service.Id,
            CustomerName = "Test",
            CustomerEmail = "test@example.com",
            StartTime = DateTime.UtcNow.AddDays(1)
        };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/appointments", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNoContent_WhenAdmin()
    {
        var service = await SeedServiceAsync();
        using var db = _factory.CreateDbContext();
        var appointment = new Appointment
        {
            ServiceItemId = service.Id,
            CustomerName = "Test",
            CustomerEmail = "test@example.com",
            StartTime = new DateTime(2025, 12, 20, 9, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2025, 12, 20, 10, 0, 0, DateTimeKind.Utc),
            Status = AppointmentStatus.Pending
        };
        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        var dto = new UpdateAppointmentStatusDto { Status = AppointmentStatus.Confirmed };
        var response = await _factory.CreateAdminClient()
            .PatchAsJsonAsync($"/api/appointments/{appointment.Id}/status", dto);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var verifyDb = _factory.CreateDbContext();
        var updated = await verifyDb.Appointments.FindAsync(appointment.Id);
        Assert.Equal(AppointmentStatus.Confirmed, updated!.Status);
    }

    [Fact]
    public async Task GetAll_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var response = await _factory.CreateClient().GetAsync("/api/appointments");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_ReturnsNotFound_WhenAppointmentDoesNotExist()
    {
        var dto = new UpdateAppointmentStatusDto { Status = AppointmentStatus.Confirmed };
        var response = await _factory.CreateAdminClient()
            .PatchAsJsonAsync("/api/appointments/99999/status", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Book_ReturnsBadRequest_WhenRequiredFieldsMissing()
    {
        var dto = new CreateAppointmentDto
        {
            ServiceItemId = 1,
            CustomerName = "",
            CustomerEmail = "not-an-email",
            StartTime = default
        };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/appointments", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
