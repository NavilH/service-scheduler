namespace ServiceScheduler.Shared.Models;

public class Appointment
{
    public int Id { get; set; }
    public int ServiceItemId { get; set; }
    public ServiceItem ServiceItem { get; set; } = null!;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AppointmentStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}
