using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BaseOps.API.Controllers;

[ApiController]
[Route("api/production")]
[Authorize(Policy = "ProductionPlannerAccess")]
public sealed class ProductionController : ControllerBase
{
    [HttpGet("dashboard")]
    public IActionResult GetDashboard() => Ok(new { totalProjects = 0, activeProjects = 0, completedProjects = 0, delayedProjects = 0, averageProgress = 0 });
}
