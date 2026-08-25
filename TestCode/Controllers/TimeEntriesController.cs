using Microsoft.AspNetCore.Mvc;
using TestCode.DTOs;
using TestCode.Services;

namespace TestCode.Controllers
{
    [ApiController]
    [Route("api/time-entries")]
    public class TimeEntriesController : ControllerBase
    {
        private readonly TimeEntryService _service;

        public TimeEntriesController(TimeEntryService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<TimeEntryListResponse>> Get(
    [FromQuery] TimeEntryQuery query)
        {
            var result = await _service.GetAsync(query);

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<TimeEntryResponse>> Create(
    TimeEntryRequest request)
        {
            var result = await _service.CreateAsync(request);

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TimeEntryResponse>> Update(
            string id,
            TimeEntryRequest request)
        {
            var result = await _service.UpdateAsync(id, request);

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            string id,
            int version)
        {
            await _service.DeleteAsync(id, version);

            return NoContent();
        }
    }
}