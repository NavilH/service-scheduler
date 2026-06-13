using System.ComponentModel.DataAnnotations;

namespace ServiceScheduler.Shared.DTOs;

public class LoginDto
{
    [Required] [EmailAddress] [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required] [MaxLength(100)]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class AvailabilityWindowDto
{
    public int Id { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsActive { get; set; }
}

public class CreateAvailabilityWindowDto
{
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
