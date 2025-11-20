using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TMSBilling.Services;

namespace TMSBilling.Controllers
{
    [ApiController]
    [Route("sync")]
    public class SyncController : ControllerBase
    {
        private readonly SyncronizeWithMcEasy _sync;

        public SyncController(SyncronizeWithMcEasy sync)
        {
            _sync = sync;
        }

        [HttpGet("order")]
        [AllowAnonymous]
        public async Task<IActionResult> SyncOrder(int limit = 1000)
        {
            try
            {
                var orders = await _sync.FetchOrderFromApi(limit);

                if (orders != null && orders.Any())
                {
                    await _sync.SyncOrderToDatabase(orders);
                }

                return Ok(new { success = true, count = orders?.Count ?? 0 });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }


        [HttpGet("job")]
        public async Task<IActionResult> SyncFO(int limit = 10)
        {
            var data = await _sync.FetchFO(limit);
            var updated = await _sync.SyncFOToDatabase(data);

            return Ok(new
            {
                success = true,
                count = data.Count,
                updated
            });
        }

    }
}
