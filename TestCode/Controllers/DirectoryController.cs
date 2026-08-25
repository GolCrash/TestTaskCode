using Microsoft.AspNetCore.Mvc;
using TestCode.DTOs;
using TestCode.Services;

namespace TestCode.Controllers
{
    [ApiController]
    [Route("api")]
    public class DirectoryController : ControllerBase
    {
        private readonly TimeEntryService _service;

        public DirectoryController(TimeEntryService service)
        {
            _service = service;
        }

        [HttpGet("employees")]
        public async Task<ActionResult<List<EmployeeResponse>>> GetEmployees()
        {
            return Ok(await _service.GetEmployeesAsync());
        }

        [HttpGet("projects")]
        public async Task<ActionResult<List<ProjectResponse>>> GetProjects()
        {
            return Ok(await _service.GetProjectsAsync());
        }
    }
}