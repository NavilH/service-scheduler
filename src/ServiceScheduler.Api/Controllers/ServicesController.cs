using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Api.Data;
using ServiceScheduler.Shared.DTOs;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Controllers;

[ApiController]
[Route("api/services")]
public class ServicesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        if (includeInactive && !User.IsInRole("Admin"))
            return Forbid();

        var items = await db.ServiceItems
            .Where(s => includeInactive || s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new ServiceItemDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                DurationMinutes = s.DurationMinutes,
                Price = s.Price,
                IsActive = s.IsActive
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.ServiceItems.FindAsync(id);
        if (item is null) return NotFound();

        return Ok(new ServiceItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            DurationMinutes = item.DurationMinutes,
            Price = item.Price,
            IsActive = item.IsActive
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateServiceItemDto dto)
    {
        var item = new ServiceItem
        {
            Name = dto.Name,
            Description = dto.Description,
            DurationMinutes = dto.DurationMinutes,
            Price = dto.Price
        };

        db.ServiceItems.Add(item);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ServiceItemDto
        {
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            DurationMinutes = item.DurationMinutes,
            Price = item.Price,
            IsActive = item.IsActive
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateServiceItemDto dto)
    {
        var item = await db.ServiceItems.FindAsync(id);
        if (item is null) return NotFound();

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.DurationMinutes = dto.DurationMinutes;
        item.Price = dto.Price;
        item.IsActive = dto.IsActive;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.ServiceItems.FindAsync(id);
        if (item is null) return NotFound();

        if (await db.Appointments.AnyAsync(a => a.ServiceItemId == id && a.Status != AppointmentStatus.Cancelled))
            return Conflict("Cannot delete a service with active appointments.");

        item.IsActive = false;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
