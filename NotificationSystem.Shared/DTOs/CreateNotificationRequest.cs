using System.ComponentModel.DataAnnotations;

namespace NotificationSystem.Shared.DTOs;

public class CreateNotificationRequest
{
    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Recipient { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Subject { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;
}