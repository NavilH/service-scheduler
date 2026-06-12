using System.Net;
using System.Net.Http.Json;
using ServiceScheduler.Shared.DTOs;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Tests;

public class SlotsControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SlotsControllerTests(ApiFactory factory) => _factory = factory;

    private async Task<(ServiceItem service, AvailabilityWindow window)> SeedAsync(DayOfWeek day)
    {
        using var db = _factory.CreateDbContext();
        var service = new ServiceItem { Name = "Slot Test Service", DurationMinutes = 60, Price = 50, IsActive = true };
        var window = new AvailabilityWindow
        {
            DayOfWeek = day,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(12, 0),
            IsActive = true
        };
        db.ServiceItems.Add(service);
        db.AvailabilityWindows.Add(window);
        await db.SaveChangesAsync();
        return (service, window);
    }

    [Fact]
    public async Task GetSlots_ReturnsExpectedSlots_WhenWindowExists()
    {
        var monday = NextWeekday(DayOfWeek.Monday);
        var (service, _) = await SeedAsync(DayOfWeek.Monday);

        var response = await _factory.CreateClient()
            .GetAsync($"/api/slots?serviceId={service.Id}&date={monday:yyyy-MM-dd}");
        var slots = await response.Content.ReadFromJsonAsync<List<AvailableSlotDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, slots!.Count); // 9-10, 10-11, 11-12 (3 one-hour slots)
        Assert.All(slots, s => Assert.Equal(60, (s.EndTime - s.StartTime).TotalMinutes));
    }

    [Fact]
    public async Task GetSlots_ExcludesBookedSlots()
    {
        var tuesday = NextWeekday(DayOfWeek.Tuesday);
        var (service, _) = await SeedAsync(DayOfWeek.Tuesday);

        using var db = _factory.CreateDbContext();
        db.Appointments.Add(new Appointment
        {
            ServiceItemId = service.Id,
            CustomerName = "Blocker",
            CustomerEmail = "blocker@test.com",
            StartTime = tuesday.ToDateTime(new TimeOnly(9, 0)),
            EndTime = tuesday.ToDateTime(new TimeOnly(10, 0)),
            Status = AppointmentStatus.Confirmed
        });
        await db.SaveChangesAsync();

        var response = await _factory.CreateClient()
            .GetAsync($"/api/slots?serviceId={service.Id}&date={tuesday:yyyy-MM-dd}");
        var slots = await response.Content.ReadFromJsonAsync<List<AvailableSlotDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, slots!.Count); // 9am slot is taken; 10-11, 11-12 remain
        Assert.DoesNotContain(slots, s => s.StartTime.Hour == 9);
    }

    [Fact]
    public async Task GetSlots_CancelledAppointments_DoNotBlockSlots()
    {
        var wednesday = NextWeekday(DayOfWeek.Wednesday);
        var (service, _) = await SeedAsync(DayOfWeek.Wednesday);

        using var db = _factory.CreateDbContext();
        db.Appointments.Add(new Appointment
        {
            ServiceItemId = service.Id,
            CustomerName = "Cancelled",
            CustomerEmail = "cancelled@test.com",
            StartTime = wednesday.ToDateTime(new TimeOnly(9, 0)),
            EndTime = wednesday.ToDateTime(new TimeOnly(10, 0)),
            Status = AppointmentStatus.Cancelled
        });
        await db.SaveChangesAsync();

        var response = await _factory.CreateClient()
            .GetAsync($"/api/slots?serviceId={service.Id}&date={wednesday:yyyy-MM-dd}");
        var slots = await response.Content.ReadFromJsonAsync<List<AvailableSlotDto>>();

        Assert.Equal(3, slots!.Count); // cancelled doesn't block the slot
    }

    [Fact]
    public async Task GetSlots_ReturnsEmpty_WhenNoWindowForDay()
    {
        using var db = _factory.CreateDbContext();
        var service = new ServiceItem { Name = "No Window Service", DurationMinutes = 60, Price = 50, IsActive = true };
        db.ServiceItems.Add(service);
        await db.SaveChangesAsync();

        var sunday = NextWeekday(DayOfWeek.Sunday);
        var response = await _factory.CreateClient()
            .GetAsync($"/api/slots?serviceId={service.Id}&date={sunday:yyyy-MM-dd}");
        var slots = await response.Content.ReadFromJsonAsync<List<AvailableSlotDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(slots!);
    }

    private static DateOnly NextWeekday(DayOfWeek day)
    {
        var date = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        while (date.DayOfWeek != day)
            date = date.AddDays(1);
        return date;
    }
}
