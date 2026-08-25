using Microsoft.AspNetCore.Mvc;
using TestCode.Services;

namespace TestCode.Controllers
{
    [ApiController]
    [Route("api/reports/projects")]
    public class ProjectReportController : ControllerBase
    {
        private readonly ProjectReportService _service;

        public ProjectReportController(ProjectReportService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] int year,
            [FromQuery] int month)
        {
            var result = await _service.GetAsync(year, month);

            return Ok(result);
        }
    }
}