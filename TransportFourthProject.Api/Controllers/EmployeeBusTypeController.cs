using Microsoft.AspNetCore.Mvc;
using TransportFourthProject.Api.Data;
using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.DTOs.BusType;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/employee/TypeBus")]
    [ApiController]
    [Authorize(Policy = AppPolicies.StaffOrManager)]
    public class EmployeeBusTypeController : ControllerBase
    {
        private readonly IEmployeeBusTypeRepository _busTypeRepo;
        private readonly AppDbContext _context;

        public EmployeeBusTypeController(IEmployeeBusTypeRepository busTypeRepo, AppDbContext context)
        {
            _busTypeRepo = busTypeRepo;
            _context = context;
        }

        [HttpGet("all-bus-types")]
        public async Task<IActionResult> GetBusTypes()
        {
            var types = await _busTypeRepo.GetAllBusTypesAsync();
            return Ok(types);
        }

        [HttpGet("all-deleted-bus-types")]
        public async Task<IActionResult> GetDeletedBusTypes()
        {
            var types = await _busTypeRepo.GetAllDeletedBusTypesAsync();
            return Ok(types);
        }

        [HttpPost("add-bus-type")]
        public async Task<IActionResult> AddBusType([FromBody] AddBusTypeForEmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var message = await _busTypeRepo.AddBusTypeAsync(dto);

            if (message == "Bus type already exists")
                return BadRequest(message);

            return Ok(message);
        }

        [HttpDelete("delete-bus-type/{busTypeId}")]
        public async Task<IActionResult> DeleteBusType(int busTypeId)
        {
            var message = await _busTypeRepo.DeleteBusTypeAsync(busTypeId);

            if (message.Contains("not found"))
                return NotFound(message);

            if (message.Contains("already"))
                return BadRequest(message);

            return Ok(message);
        }

        [HttpPut("restore-bus-type/{busTypeId}")]
        public async Task<IActionResult> RestoreBusType(int busTypeId)
        {
            var message = await _busTypeRepo.RestoreBusTypeAsync(busTypeId);

            if (message.Contains("not found"))
                return NotFound(message);

            if (message.Contains("already"))
                return BadRequest(message);

            return Ok(message);
        }
    }
}
