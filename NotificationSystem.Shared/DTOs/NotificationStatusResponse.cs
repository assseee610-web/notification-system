namespace NotificationSystem.Shared.DTOs;

public class NotificationStatusResponse
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<HistoryItem> History { get; set; } = new();
}

public class HistoryItem
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime AttemptedAt { get; set; }
}