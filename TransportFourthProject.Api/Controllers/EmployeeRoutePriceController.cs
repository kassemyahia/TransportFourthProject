using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.DTOs.RoutePrice;
using TransportFourthProject.Api.Models;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/employee/route-price")]
    [ApiController]
    [Authorize(Policy = AppPolicies.StaffOrManager)]
    public class EmployeeRoutePriceController : ControllerBase
    {
        private readonly IEmployeeRoutePriceRepository _routePriceRepository;

        public EmployeeRoutePriceController(IEmployeeRoutePriceRepository routePriceRepository)
        {
            _routePriceRepository = routePriceRepository;
        }
        [HttpGet("all-available-route-prices")]
        public async Task<IActionResult> GetAllActiveRoutes()
        {
            var routes = await _routePriceRepository.GetAllRoutePricesAsync();
            return Ok(routes);
        }
        [HttpGet("all-deleted-route-prices")]
        public async Task<IActionResult> GetAllDeletedRoutes()
        {
            var routes = await _routePriceRepository.GetDeletedRoutePricesAsync();
            return Ok(routes);
        }

        [HttpPatch("update-route-price/{routePriceId}")]
        public async Task<IActionResult> PatchRoutePrice(int routePriceId, JsonPatchDocument<RoutePrice> patchDoc)
        {
            var message = await _routePriceRepository.PatchRoutePriceAsync(routePriceId, patchDoc);

            if (message.Contains("not found"))
                return NotFound(message);

            if (message.Contains("Cannot"))
                return BadRequest(message);

            return Ok(message);
        }

        [HttpDelete("delete-route-price/{id}")]
        public async Task<IActionResult> DeleteRoutePrice(int id)
        {
            var message = await _routePriceRepository.DeleteRoutePriceAsync(id);

            if (message.Contains("not found"))
                return NotFound(message);

            if (message.Contains("Cannot"))
                return BadRequest(message);

            return Ok(message);
        }

        [HttpPut("restore-route-price/{id}")]
        public async Task<IActionResult> RestoreRoutePrice(int id)
        {
            var message = await _routePriceRepository.RestoreRoutePriceAsync(id);

            if (message.Contains("not found"))
                return NotFound(message);

            if (message.Contains("not deleted"))
                return BadRequest(message);

            return Ok(message);
        }

        [HttpPost("add-route-price")]
        public async Task<IActionResult> AddRoutePrice(AddRoutePriceForEmployeeDto dto)
        {
            var message = await _routePriceRepository.AddRoutePriceAsync(dto);

            if (message.Contains("not found"))
                return NotFound(message);

            if (message.Contains("exists"))
                return BadRequest(message);

            return Ok(message);
        }

        [HttpGet("route-price-statuses")]
        public async Task<IActionResult> GetRoutePriceStatuses()
        {
            var stats = await _routePriceRepository.GetRoutePriceStatusesAsync();
            return Ok(stats);
        }

        [HttpGet("all-route-prices-by-bus-type/{busTypeId}")]
        public async Task<IActionResult> GetAllRoutePricesByBusTypeId(int busTypeId)
        {
            var routes = await _routePriceRepository.GetAllRoutePricesByBusTypeIdAsync(busTypeId);
            return Ok(routes);
        }

        [HttpGet("most-used-route-prices")]
        public async Task<IActionResult> GetMostActiveUsedRoutePrices()
        {
            var routes = await _routePriceRepository.GetMostActiveUsedRoutePricesAsync();
            return Ok(routes);
        }
        [HttpGet("least-used-route-prices")]
        public async Task<IActionResult> GetLeastActiveUsedRoutePrices()
        {
            var routes = await _routePriceRepository.GetLeastActiveUsedRoutePricesAsync();
            return Ok(routes);
        }

        [HttpGet("suggest-price-for-route-price")]
        public IActionResult SuggestPriceForRoutePrice(decimal distanceKm, int capacity)
        {
            try
            {
                var price = _routePriceRepository.SuggestPriceForRoutePrice(distanceKm, capacity);
                return Ok(new { suggestedPriceForRoutePrice = price });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("suggest-route-price-for-deletion")]
        public async Task<IActionResult> SuggestRoutePricesForDeletion()
        {
            var routes = await _routePriceRepository.SuggestRoutePricesForDeletionAsync();
            return Ok(routes);
        }

        [HttpGet("update-price-for-route-price-suggest")]
        public async Task<IActionResult> UpdatePriceForRoutePriceSuggest()
        {
            var routes = await _routePriceRepository.UpdatePriceForRoutePriceSuggestAsync();
            return Ok(routes);
        }

    }
}







