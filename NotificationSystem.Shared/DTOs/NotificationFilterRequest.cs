namespace NotificationSystem.Shared.DTOs;

public class NotificationFilterRequest
{
    public string? Recipient { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}