using System.ComponentModel.DataAnnotations;
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
    [Range(1, int.MaxValue, ErrorMessage = "A valid service must be selected.")]
    public int ServiceItemId { get; set; }

    [Required] [MaxLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required] [EmailAddress] [MaxLength(200)]
    public string CustomerEmail { get; set; } = string.Empty;

    [Phone] [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateAppointmentStatusDto
{
    [EnumDataType(typeof(AppointmentStatus))]
    public AppointmentStatus Status { get; set; }
}

public class AvailableSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
