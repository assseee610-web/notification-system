using System.ComponentModel.DataAnnotations;

namespace NotificationSystem.Shared.Models;

public class ArchivedNotification
{
    [Key]
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Recipient { get; set; } = string.Empty;

    public string? Subject { get; set; }

    public string Body { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int RetryCount { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
}