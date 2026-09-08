using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppPolicies.UserOnly)]
    public class SortOptionsController : ControllerBase
    {
        [HttpGet("order")]
        public IActionResult GetOrderOptions()
        {
            var options = new[]
            {
                new { Key = "asc", Label = "Ascending" },
                new { Key = "desc", Label = "Descending" }
            };

            return Ok(options);
        }
    }
}
