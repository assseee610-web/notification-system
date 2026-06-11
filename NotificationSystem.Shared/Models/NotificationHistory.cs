using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationSystem.Shared.Models;

public class NotificationHistory
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid NotificationId { get; set; }

    [ForeignKey("NotificationId")]
    public Notification? Notification { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}