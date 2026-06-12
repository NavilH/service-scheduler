using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Api.Data;
using ServiceScheduler.Shared.DTOs;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Controllers;

[ApiController]
[Route("api/availability")]
public class AvailabilityController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var windows = await db.AvailabilityWindows
            .Where(w => w.IsActive)
            .OrderBy(w => w.DayOfWeek).ThenBy(w => w.StartTime)
            .Select(w => new AvailabilityWindowDto
            {
                Id = w.Id,
                DayOfWeek = w.DayOfWeek,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                IsActive = w.IsActive
            })
            .ToListAsync();

        return Ok(windows);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateAvailabilityWindowDto dto)
    {
        var window = new AvailabilityWindow
        {
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };

        db.AvailabilityWindows.Add(window);
        await db.SaveChangesAsync();

        return Ok(new AvailabilityWindowDto
        {
            Id = window.Id,
            DayOfWeek = window.DayOfWeek,
            StartTime = window.StartTime,
            EndTime = window.EndTime,
            IsActive = window.IsActive
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var window = await db.AvailabilityWindows.FindAsync(id);
        if (window is null) return NotFound();

        window.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
