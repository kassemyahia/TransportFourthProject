using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/admin/trips")]
    [ApiController]
    [Authorize(Policy = AppPolicies.StaffOrManager)]
    public class AdminTripController : ControllerBase
    {
        private readonly IAdminTripRepo _adminTripRepo;
        private readonly AppDbContext _context;
        public AdminTripController(IAdminTripRepo adminTripRepo, AppDbContext context)
        {
            _adminTripRepo = adminTripRepo;
            _context = context;
        }

        [HttpGet("trip/{tripId}/confirmed-passengers")]
        public async Task<IActionResult> GetConfirmedPassengers(int tripId)
        {
            var result = await _adminTripRepo.GetConfirmedPassengersAsync(tripId);
            return Ok(result);
        }

        [HttpGet("get-all-employees")]
        [Authorize(Policy = AppPolicies.ManagerOnly)]
        public async Task<IActionResult> GetAllEmployees()
        {
            var employees = await _context.Employees
                .Where(e => e.Role == EmployeeRole.OfficeEmployee)
                .Select(e => new
                {
                    e.Id,
                    e.FirstName,
                    e.LastName,
                    e.Phone,
                    e.Salary,
                    e.Role,
                    e.Status,
                    e.HireDate
                })
                .ToListAsync();

            return Ok(employees);
        }

        [HttpGet("drivers")]
        [Authorize(Policy = AppPolicies.ManagerOnly)]
        public async Task<IActionResult> GetAllDrivers()
        {
            var drivers = await _context.Employees
                .Where(e => e.Role == EmployeeRole.Driver)
                .Select(e => new
                {
                    e.Id,
                    e.FirstName,
                    e.LastName,
                    e.Phone,
                    e.LicenseNumber,
                    e.Status,
                    e.Salary,
                    e.HireDate
                })
                .ToListAsync();

            return Ok(drivers);
        }
    }
}
