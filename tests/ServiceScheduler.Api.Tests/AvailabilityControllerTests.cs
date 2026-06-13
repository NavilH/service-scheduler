using System.Net;
using System.Net.Http.Json;
using ServiceScheduler.Shared.DTOs;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Tests;

public class AvailabilityControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AvailabilityControllerTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetAll_ReturnsOnlyActiveWindows()
    {
        using var db = _factory.CreateDbContext();
        db.AvailabilityWindows.AddRange(
            new AvailabilityWindow { DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0), IsActive = true },
            new AvailabilityWindow { DayOfWeek = DayOfWeek.Sunday, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(12, 0), IsActive = false });
        await db.SaveChangesAsync();

        var response = await _factory.CreateClient().GetAsync("/api/availability");
        var windows = await response.Content.ReadFromJsonAsync<List<AvailabilityWindowDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(windows!, w => Assert.True(w.IsActive));
        Assert.DoesNotContain(windows!, w => w.DayOfWeek == DayOfWeek.Sunday);
    }

    [Fact]
    public async Task Create_ReturnsOk_WhenAdmin()
    {
        var dto = new CreateAvailabilityWindowDto
        {
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(16, 0)
        };

        var response = await _factory.CreateAdminClient().PostAsJsonAsync("/api/availability", dto);
        var created = await response.Content.ReadFromJsonAsync<AvailabilityWindowDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(DayOfWeek.Wednesday, created!.DayOfWeek);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var dto = new CreateAvailabilityWindowDto
        {
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(17, 0)
        };

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/availability", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletes_WhenAdmin()
    {
        using var db = _factory.CreateDbContext();
        var window = new AvailabilityWindow
        {
            DayOfWeek = DayOfWeek.Thursday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(14, 0),
            IsActive = true
        };
        db.AvailabilityWindows.Add(window);
        await db.SaveChangesAsync();

        var response = await _factory.CreateAdminClient().DeleteAsync($"/api/availability/{window.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var verifyDb = _factory.CreateDbContext();
        var updated = await verifyDb.AvailabilityWindows.FindAsync(window.Id);
        Assert.False(updated!.IsActive);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenWindowDoesNotExist()
    {
        var response = await _factory.CreateAdminClient().DeleteAsync("/api/availability/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var response = await _factory.CreateClient().DeleteAsync("/api/availability/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
