using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceScheduler.Api.Data;
using ServiceScheduler.Api.Services;
using ServiceScheduler.Shared.DTOs;
using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Api.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentsController(AppDbContext db, EmailService emailService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await db.Appointments
            .Include(a => a.ServiceItem)
            .OrderBy(a => a.StartTime)
            .Select(a => ToDto(a))
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var a = await db.Appointments.Include(a => a.ServiceItem).FirstOrDefaultAsync(a => a.Id == id);
        if (a is null) return NotFound();
        return Ok(ToDto(a));
    }

    [HttpPost]
    public async Task<IActionResult> Book(CreateAppointmentDto dto)
    {
        var service = await db.ServiceItems.FindAsync(dto.ServiceItemId);
        if (service is null || !service.IsActive)
            return BadRequest("Service not found or inactive.");

        var endTime = dto.StartTime.AddMinutes(service.DurationMinutes);

        var conflict = await db.Appointments.AnyAsync(a =>
            a.Status != AppointmentStatus.Cancelled &&
            a.StartTime < endTime &&
            a.EndTime > dto.StartTime);

        if (conflict)
            return Conflict("The selected time slot is unavailable.");

        var appointment = new Appointment
        {
            ServiceItemId = dto.ServiceItemId,
            CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail,
            CustomerPhone = dto.CustomerPhone,
            StartTime = dto.StartTime,
            EndTime = endTime,
            Notes = dto.Notes,
            Status = AppointmentStatus.Pending
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        _ = emailService.SendBookingConfirmationAsync(
            appointment.CustomerEmail,
            appointment.CustomerName,
            service.Name,
            appointment.StartTime
        );

        return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, ToDto(appointment));
    }

    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateAppointmentStatusDto dto)
    {
        var appointment = await db.Appointments
            .Include(a => a.ServiceItem)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (appointment is null) return NotFound();

        appointment.Status = dto.Status;
        await db.SaveChangesAsync();

        _ = emailService.SendStatusUpdateAsync(
            appointment.CustomerEmail,
            appointment.CustomerName,
            appointment.ServiceItem.Name,
            appointment.StartTime,
            dto.Status
        );

        return NoContent();
    }

    private static AppointmentDto ToDto(Appointment a) => new()
    {
        Id = a.Id,
        ServiceItemId = a.ServiceItemId,
        ServiceName = a.ServiceItem?.Name ?? string.Empty,
        CustomerName = a.CustomerName,
        CustomerEmail = a.CustomerEmail,
        CustomerPhone = a.CustomerPhone,
        StartTime = a.StartTime,
        EndTime = a.EndTime,
        Status = a.Status,
        Notes = a.Notes,
        CreatedAt = a.CreatedAt
    };
}
