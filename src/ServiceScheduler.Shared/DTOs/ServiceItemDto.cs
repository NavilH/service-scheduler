using System.ComponentModel.DataAnnotations;

namespace ServiceScheduler.Shared.DTOs;

public class ServiceItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
}

public class CreateServiceItemDto
{
    [Required] [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes.")]
    public int DurationMinutes { get; set; }

    [Range(0, 10000, ErrorMessage = "Price must be between $0 and $10,000.")]
    public decimal Price { get; set; }
}

public class UpdateServiceItemDto
{
    [Required] [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes.")]
    public int DurationMinutes { get; set; }

    [Range(0, 10000, ErrorMessage = "Price must be between $0 and $10,000.")]
    public decimal Price { get; set; }

    public bool IsActive { get; set; }
}
