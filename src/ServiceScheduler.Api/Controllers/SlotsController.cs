using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Api.Data;
using ServiceScheduler.Shared.DTOs;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Controllers;

[ApiController]
[Route("api/slots")]
public class SlotsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int serviceId,
        [FromQuery] DateOnly date,
        [FromQuery] string timeZone = "UTC")
    {
        TimeZoneInfo tz;
        try { tz = TimeZoneInfo.FindSystemTimeZoneById(timeZone); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return BadRequest($"Unknown time zone: '{timeZone}'.");
        }

        var service = await db.ServiceItems.FindAsync(serviceId);
        if (service is null || !service.IsActive)
            return BadRequest("Service not found or inactive.");

        var window = await db.AvailabilityWindows
            .Where(w => w.IsActive && w.DayOfWeek == date.DayOfWeek)
            .FirstOrDefaultAsync();

        if (window is null)
            return Ok(Array.Empty<AvailableSlotDto>());

        // Interpret availability window times in the requested timezone, then convert to UTC
        var windowStartUtc = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(window.StartTime), tz);
        var windowEndUtc = TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(window.EndTime), tz);

        // Load only appointments that overlap this UTC window
        var existingAppointments = await db.Appointments
            .Where(a =>
                a.Status != AppointmentStatus.Cancelled &&
                a.StartTime < windowEndUtc &&
                a.EndTime > windowStartUtc)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var slots = new List<AvailableSlotDto>();
        var slotStart = windowStartUtc;

        while (slotStart.AddMinutes(service.DurationMinutes) <= windowEndUtc)
        {
            var slotEnd = slotStart.AddMinutes(service.DurationMinutes);

            var hasConflict = existingAppointments.Any(a =>
                a.StartTime < slotEnd && a.EndTime > slotStart);

            if (!hasConflict)
                slots.Add(new AvailableSlotDto
                {
                    StartTime = DateTime.SpecifyKind(slotStart, DateTimeKind.Utc),
                    EndTime = DateTime.SpecifyKind(slotEnd, DateTimeKind.Utc)
                });

            slotStart = slotStart.AddMinutes(service.DurationMinutes);
        }

        return Ok(slots);
    }
}
