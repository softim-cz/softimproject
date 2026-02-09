using Microsoft.AspNetCore.Mvc;
using SoftimProject.Application.Features.Export;

namespace SoftimProject.WebApi.Controllers;

[Route("api/v{version:apiVersion}/exports")]
public class ExportController : ApiControllerBase
{
    [HttpPost("xlsx")]
    public async Task<IActionResult> ExportXlsx(ExportXlsxQuery query)
    {
        var bytes = await Mediator.Send(query);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"export-{query.ViewType}.xlsx");
    }
}
