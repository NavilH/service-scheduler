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
    public async Task<IActionResult> GetAvailableSlots([FromQuery] int serviceId, [FromQuery] DateOnly date)
    {
        var service = await db.ServiceItems.FindAsync(serviceId);
        if (service is null || !service.IsActive)
            return BadRequest("Service not found or inactive.");

        var window = await db.AvailabilityWindows
            .Where(w => w.IsActive && w.DayOfWeek == date.DayOfWeek)
            .FirstOrDefaultAsync();

        if (window is null)
            return Ok(Array.Empty<AvailableSlotDto>());

        var existingAppointments = await db.Appointments
            .Where(a =>
                a.Status != AppointmentStatus.Cancelled &&
                DateOnly.FromDateTime(a.StartTime) == date)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync();

        var slots = new List<AvailableSlotDto>();
        var slotStart = date.ToDateTime(window.StartTime);
        var windowEnd = date.ToDateTime(window.EndTime);

        while (slotStart.AddMinutes(service.DurationMinutes) <= windowEnd)
        {
            var slotEnd = slotStart.AddMinutes(service.DurationMinutes);

            var hasConflict = existingAppointments.Any(a =>
                a.StartTime < slotEnd && a.EndTime > slotStart);

            if (!hasConflict)
                slots.Add(new AvailableSlotDto { StartTime = slotStart, EndTime = slotEnd });

            slotStart = slotStart.AddMinutes(service.DurationMinutes);
        }

        return Ok(slots);
    }
}
