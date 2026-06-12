using ServiceScheduler.Shared.Models;

namespace ServiceScheduler.Shared.DTOs;

public class AppointmentDto
{
    public int Id { get; set; }
    public int ServiceItemId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateAppointmentDto
{
    public int ServiceItemId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateTime StartTime { get; set; }
    public string? Notes { get; set; }
}

public class UpdateAppointmentStatusDto
{
    public AppointmentStatus Status { get; set; }
}

public class AvailableSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
