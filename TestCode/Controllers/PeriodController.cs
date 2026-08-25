using Microsoft.AspNetCore.Mvc;
using TestCode.DTOs;
using TestCode.Services;

namespace TestCode.Controllers
{
    [ApiController]
    [Route("api/periods")]
    public class PeriodController : ControllerBase
    {
        private readonly PeriodService _service;

        public PeriodController(PeriodService service)
        {
            _service = service;
        }

        [HttpPost("close")]
        public async Task<IActionResult> Close(PeriodRequest request)
        {
            await _service.CloseAsync(request);

            return NoContent();
        }

        [HttpPost("open")]
        public async Task<IActionResult> Open(PeriodRequest request)
        {
            await _service.OpenAsync(request);

            return NoContent();
        }
    }
}