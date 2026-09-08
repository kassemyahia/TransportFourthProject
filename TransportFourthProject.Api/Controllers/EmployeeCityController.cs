using Microsoft.AspNetCore.Mvc;
using TransportFourthProject.Api.Data;
using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.DTOs.City;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/employee/city")]
    [ApiController]
    [Authorize(Policy = AppPolicies.StaffOrManager)]
    public class EmployeeCityController : ControllerBase
    {
        private readonly IEmployeeCityRepository _cityRepository;

        public EmployeeCityController(IEmployeeCityRepository cityRepository)
        {
            _cityRepository = cityRepository;
        }

        [HttpGet("all-cities")]
        public async Task<IActionResult> GetCities()
        {
            var cities = await _cityRepository.GetAllCitiesAsync();
            var result  = cities.Select(c => new CityDto
            {
                Id = c.Id,
                Name = c.Name
            });
            return Ok(result);
        }

        [HttpPost("add-city")]
        public async Task<IActionResult> AddCity(AddCityForEmployeeDto dto)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var message = await _cityRepository.AddCityAsync(dto);

            if (message.Contains("exists"))
                return BadRequest(message);

            return Ok(message);
        }

        [HttpPut("update-city/{id}")]
        public async Task<IActionResult> UpdateCity(int id, UpdateCityForEmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var message = await _cityRepository.UpdateCityAsync(id, dto);

            if (message.Contains("not found"))
                return NotFound(message);

            if (message.Contains("exists"))
                return BadRequest(message);

            return Ok(message);
        }



        [HttpGet("cities/least-used-trips")]
        public async Task<IActionResult> LeastUsedCitiesTrips()
        {
            return Ok(await _cityRepository.GetLeastUsedCitiesByTripsAsync());
        }

        [HttpGet("cities/most-used-trips")]
        public async Task<IActionResult> MostUsedCitiesTrips()
        {
            return Ok(await _cityRepository.GetMostUsedCitiesByTripsAsync());
        }

        [HttpGet("cities/most-used-routes")]
        public async Task<IActionResult> MostUsedCitiesRoutes()
        {
            return Ok(await _cityRepository.GetMostUsedCitiesByRoutesAsync());
        }

        [HttpGet("cities/least-used-routes")]
        public async Task<IActionResult> LeastUsedCitiesRoutes()
        {
            return Ok(await _cityRepository.GetLeastUsedCitiesByRoutesAsync());
        }

        [HttpGet("cities/most-travel-month")]
        public async Task<IActionResult> MostTravelCitiesMonth(int month, int year)
        {
            try
            {
                var result = await _cityRepository.GetMostTravelCitiesInMonthAsync(month, year);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new {error = ex.Message});
            }
        }
    }
}
