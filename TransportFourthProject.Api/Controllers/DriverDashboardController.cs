using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using TransportFourthProject.Api.Authorization;
using TransportFourthProject.Api.DTOs.Employee;
using TransportFourthProject.Api.Repositories;

namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/driver-dashboard")]
    [Route("api/driver-hashboard")]
    [Authorize(Policy = AppPolicies.DriverOnly)]
    public class DriverDashboardController : ControllerBase
    {
        private readonly IDriverDashboardRepo _driverDashboardRepo;

        public DriverDashboardController(IDriverDashboardRepo repo)
        {
            _driverDashboardRepo = repo;
        }

        private bool TryGetDriverId(out int driverId)
        {
            var driverIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(driverIdClaim, out driverId);
        }

        [HttpGet("today-trip-driver")]
        public async Task<IActionResult> GetTodayTrips()
        {
            if (!TryGetDriverId(out var driverId))
                return Unauthorized(new { Message = "Invalid driver token" });

            var result =
                await _driverDashboardRepo.GetTodayTripsAsync(driverId);

            return Ok(result);
        }

        [HttpGet("tomorrow-trip-driver")]
        public async Task<IActionResult> GetTomorrowTrips()
        {
            if (!TryGetDriverId(out var driverId))
                return Unauthorized(new { Message = "Invalid driver token" });

            var result =
                await _driverDashboardRepo.GetTomorrowTripsAsync(driverId);

            return Ok(result);
        }

        [HttpGet("after-tomorrow")]
        public async Task<IActionResult> GetAfterTomorrowTrips()
        {
            if (!TryGetDriverId(out var driverId))
                return Unauthorized(new { Message = "Invalid driver token" });

            var result =
                await _driverDashboardRepo.GetAfterTomorrowTripsAsync(driverId);

            return Ok(result);
        }

        [HttpGet("monthly/{year}/{month}")]
        public async Task<IActionResult> GetMonthlyTripsCount(
            [Range(1, 9999)] int year,
            [Range(1, 12)] int month)
        {
            if (!TryGetDriverId(out var driverId))
                return Unauthorized(new { Message = "Invalid driver token" });

            var result =
                await _driverDashboardRepo.GetMonthlyTripsCountAsync(
                    driverId,
                    year,
                    month);

            return Ok(new DriverTripsCountDto
            {
                Year = year,
                Month = month,
                Count = result
            });
        }

        [HttpGet("yearly/{year}")]
        public async Task<IActionResult> GetYearlyTripsCount(
            [Range(1, 9999)] int year)
        {
            if (!TryGetDriverId(out var driverId))
                return Unauthorized(new { Message = "Invalid driver token" });

            var result =
                await _driverDashboardRepo.GetYearlyTripsCountAsync(
                    driverId,
                    year);

            return Ok(new DriverYearlyTripsCountDto
            {
                Year = year,
                Count = result
            });
        }
    }
}
