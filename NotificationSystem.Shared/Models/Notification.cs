using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NotificationSystem.Shared.Models;

public class Notification
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(10)]
    public string Type { get; set; } = string.Empty; // "Email", "SMS", "Push"

    [Required]
    [MaxLength(200)]
    public string Recipient { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Subject { get; set; }

    [Required]
    public string Body { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "New";

    public int RetryCount { get; set; } = 0;

    public int MaxRetries { get; set; } = 3;

    public Guid CreatedByUserId { get; set; }

    [ForeignKey("CreatedByUserId")]
    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}