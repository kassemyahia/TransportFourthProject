using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.MicrosoftExtensions;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs.Bus;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/employee/buses")]
    [ApiController]
    [Authorize(Policy = AppPolicies.StaffOrManager)]
    public class EmployeeBusController : ControllerBase
    {
        private readonly IEmployeeBusRepository _employeeBusRepo;
        private readonly AppDbContext _context;

        public EmployeeBusController(IEmployeeBusRepository employeeBusRepo, AppDbContext context)
        {
            _employeeBusRepo = employeeBusRepo;
            _context = context;
        }

        [HttpGet("all-buses")]
        public async Task<IActionResult> GetAllBuses()
        {
            var buses = await _employeeBusRepo.GetAllBusesAsync();
            return Ok(buses);
        }

        [HttpGet("search-buses")]
        public async Task<IActionResult> FilterBuses([FromQuery] BusStatus? status, [FromQuery] int? busTypeId)
        {
            var buses = await _employeeBusRepo.SearchBusesAsync(status, busTypeId);
            return Ok(buses);
        }

        [HttpGet("bus-statuses")]
        public IActionResult GetBusStatuses()
        {
            var statuses = Enum.GetValues(typeof(BusStatus))
                .Cast<BusStatus>()
                .Select(s => new
                {
                    Id = (int)s,
                    Name = s.ToString()
                })
                .ToList();
            return Ok(statuses);
        }

        [HttpPost("add-bus")]
        public async Task<IActionResult> AddBus([FromBody] AddBusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _employeeBusRepo.AddBusAsync(dto);

            if (!success)
                return BadRequest("Failed to add bus or busNumber already exists");

            return Ok("Bus added successfully");
        }

        [HttpPut("update-bus/{busId}")]
        public async Task<IActionResult> UpdateBus([Range(1, int.MaxValue, ErrorMessage = "Bus ID must be greater than 0")] int busId,
                                                    [FromBody] UpdateBusStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _employeeBusRepo.UpdateBusAsync(busId, dto);

            if (!success)
                return BadRequest("Bus not found.");

            return Ok("Bus updated successfully");
        }


        [HttpGet("buses/most-used-in-trips")]
        public async Task<IActionResult> MostUsedBuses()
        {
            return Ok(await _employeeBusRepo.GetMostUsedBusesAsync());
        }

        [HttpGet("buses/least-used-in-trips")]
        public async Task<IActionResult> LeastUsedBuses()
        {
            return Ok(await _employeeBusRepo.GetLeastUsedBusesAsync());
        }

        [HttpGet("buses/most-active-month")]
        public async Task<IActionResult> MostActiveBusesInMonth(int month, int year)
        {
            try
            {
                var result = await _employeeBusRepo.GetMostActiveBusesInMonthAsync(month, year);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("buses/suggest-deletion")]
        public async Task<IActionResult> SuggestBusDeletion()
        {
            var result = await _employeeBusRepo.SuggestBusDeletionAsync();
            return Ok(result);
        }

        [HttpGet("buses/on-roads")]
        public async Task<IActionResult> GetBusesOnRoads()
        {
            var result = await _employeeBusRepo.GetBusesOnRoadsAsync();
            return Ok(result);
        }
    }
}
