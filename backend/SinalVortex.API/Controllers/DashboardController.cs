using MediatR;
using Microsoft.AspNetCore.Mvc;
using SinalVortex.Application.Queries.Dashboard;

namespace SinalVortex.API.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
public sealed class DashboardController(ISender mediator) : ControllerBase
{
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken)
    {
        return Ok(await mediator.Send(new ObterMetricasDashboardQuery(), cancellationToken));
    }
}
