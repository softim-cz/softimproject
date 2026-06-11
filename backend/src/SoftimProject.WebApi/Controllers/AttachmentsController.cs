using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Attachments.DeleteAttachment;
using SoftimProject.Application.Features.Attachments.GetAttachments;
using SoftimProject.Application.Features.Attachments.UploadAttachment;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/projects/{projectId:guid}/tickets/{ticketId:guid}/attachments")]
public class AttachmentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AttachmentDto>>> GetAll(Guid projectId, Guid ticketId)
    {
        return Ok(await Mediator.Send(new GetAttachmentsQuery(projectId, ticketId)));
    }

    [HttpPost]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB
    public async Task<ActionResult<AttachmentDto>> Upload(Guid projectId, Guid ticketId, IFormFile file)
    {
        var attachment = await Mediator.Send(new UploadAttachmentCommand(
            projectId,
            ticketId,
            file.FileName,
            file.ContentType,
            file.Length,
            file.OpenReadStream()));
        return Ok(attachment);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid projectId, Guid ticketId, Guid id)
    {
        await Mediator.Send(new DeleteAttachmentCommand(projectId, ticketId, id));
        return NoContent();
    }
}
