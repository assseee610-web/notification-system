using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationSystem.Shared.DTOs;
using NotificationSystem.Web.Interfaces;
using System.Security.Claims;

namespace NotificationSystem.Web.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateNotificationRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null)
            return Unauthorized();

        var userId = Guid.Parse(userIdClaim);
        var result = await _notificationService.CreateNotificationAsync(request, userId);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetStatus(Guid id)
    {
        var result = await _notificationService.GetStatusAsync(id);

        if (result == null)
            return NotFound(new { message = "Уведомление не найдено" });

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string recipient,
        [FromQuery] string type,
        [FromQuery] string status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var filter = new NotificationFilterRequest
        {
            Recipient = recipient,
            Type = type,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        var result = await _notificationService.GetHistoryAsync(filter);
        return Ok(result);
    }
}