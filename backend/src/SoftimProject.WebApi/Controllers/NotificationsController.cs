using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Notifications.GetNotifications;
using SoftimProject.Application.Features.Notifications.MarkAllNotificationsRead;
using SoftimProject.Application.Features.Notifications.MarkNotificationRead;

namespace SoftimProject.WebApi.Controllers;

public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NotificationDto>>> GetAll()
    {
        return Ok(await Mediator.Send(new GetNotificationsQuery()));
    }

    [HttpPut("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId)
    {
        await Mediator.Send(new MarkNotificationReadCommand(notificationId));
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await Mediator.Send(new MarkAllNotificationsReadCommand());
        return NoContent();
    }
}
