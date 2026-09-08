using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs.BusType;
using TransportFourthProject.Api.DTOs.City;
using TransportFourthProject.Api.DTOs.Trip;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Models;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;
using Microsoft.EntityFrameworkCore;
namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppPolicies.UserOnly)]
    public class TripController : ControllerBase
    {
        private readonly ITripRepository _tripRepo;
        private readonly AppDbContext _context;

        public TripController(ITripRepository tripRepo, AppDbContext context)
        {
            _tripRepo = tripRepo;
            _context = context;
        }

        [HttpGet("all-trips")]
        public async Task<IActionResult> GetAllTrips()
        {
            var trips = await _tripRepo.GetAllTripsAsync();
            if (!trips.Any())
                return Ok(new { Message = "No trips available at the moment." });

            var result = trips.Select(t => new TripListDto
            {
                TripId = t.Id,
                DepartureTime = t.DepartureTime.ToString("yyyy-MM-dd HH:mm"),
                StartCity = t.RoutePrice.StartCity.Name,
                EndCity = t.RoutePrice.EndCity.Name,
                BusType = t.Bus.BusType.Type,
                BasePrice = (t.RoutePrice.Price.ToString()) + " SYP",
                DiscountName = t.TripDiscount?.Name ?? "no discount",
                DiscountPercentage = (t.TripDiscount?.Percentage ?? 0) + "%"
            });

            return Ok(result);
        }

        [HttpGet("search-options")]
        public async Task<IActionResult> GetSearchOptions()
        {
            var cities = await _context.Cities
                .AsNoTracking()
                .OrderBy(city => city.Name)
                .Select(city => new CityDto { Id = city.Id, Name = city.Name })
                .ToListAsync();

            var busTypes = await _context.BusTypes
                .AsNoTracking()
                .Where(busType => !busType.IsDeleted)
                .OrderBy(busType => busType.Type)
                .Select(busType => new BusTypeDto
                {
                    BusTypeId = busType.Id,
                    Name = busType.Type,
                    Capacity = busType.Capacity
                })
                .ToListAsync();

            return Ok(new { Cities = cities, BusTypes = busTypes });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTrips(string? startCity, string? endCity, DateTime? date,
                                                     string? busType, string? sortBy, string? order)
        {
            bool hasTime = date.HasValue && date.Value.TimeOfDay != TimeSpan.Zero;
            bool hasMinutes = hasTime && date.Value.Minute != 0;

            var trips = await _tripRepo.SearchTripsAsync(startCity, endCity, date,
                                                        hasTime, hasMinutes, busType,
                                                        sortBy, order);

            if (!trips.Any())
                return Ok(new { Message = "No trips found" });

            var result = trips.Select(t => new TripListDto
            {
                TripId = t.Id,
                DepartureTime = t.DepartureTime.ToString("yyyy-MM-dd HH:mm"),
                StartCity = t.RoutePrice.StartCity.Name,
                EndCity = t.RoutePrice.EndCity.Name,
                BusType = t.Bus.BusType.Type,
                BasePrice = (t.RoutePrice.Price.ToString()) + " SYP",
                DiscountName = t.TripDiscount?.Name ?? "no discount",
                DiscountPercentage = (t.TripDiscount?.Percentage ?? 0) + "%"
            });

            return Ok(result);
        }


        [HttpGet("details/{tripId}")]
        public async Task<IActionResult> GetTripDetails(int tripId)
        {
            if (tripId <= 0)
                return BadRequest("Invalid trip ID");

            var trip = await _tripRepo.GetTripDetailsAsync(tripId);

            if (trip == null)
                return NotFound("Trip not found");

            if (trip.IsDeleted ||
                trip.RoutePrice.IsDeleted ||
                trip.Bus.BusType.IsDeleted ||
                trip.Bus.Status != BusStatus.Active ||
                trip.Employee.Status != EmployeeStatus.Active)
            {
                return BadRequest(new { Message = "Trip is no longer available." });
            }

            if (trip.DepartureTime <= DateTime.Now.AddHours(4))
                return BadRequest(new { Message = "Trip is no longer available." });

            if (trip.DepartureTime <= DateTime.Now)
                return BadRequest(new { Message = "Trip has already departed." });

            var bookedSeats = await _tripRepo.GetBookedSeatsAsync(tripId);

            int availableSeats = trip.Bus.BusType.Capacity - bookedSeats;

            string tripStatus = availableSeats == 0 ? "complete" : "available";

            var dto = new TripDetailsDto
            {
                TripId = trip.Id,
                DepartureTime = trip.DepartureTime.ToString("yyyy-MM-dd HH:mm"),

                StartCity = trip.RoutePrice.StartCity.Name,
                EndCity = trip.RoutePrice.EndCity.Name,

                BusNumber = trip.Bus.BusNumber,
                BusType = trip.Bus.BusType.Type,

                AvailableSeats = availableSeats,
                TripStatus = tripStatus,

                BasePrice = trip.RoutePrice.Price + " SYP",

                DriverName = $"{trip.Employee.FirstName} {trip.Employee.LastName}",

                DiscountName = trip.TripDiscount?.Name ?? "no discount",
                DiscountPercentage = (trip.TripDiscount?.Percentage ?? 0) + "%"
            };

            return Ok(dto);
        }

        [HttpGet("{tripId}/seats")]
        public async Task<IActionResult> GetTripSeats(int tripId)
        {
            if (tripId <= 0)
                return BadRequest("Invalid trip ID");

            var trip = await _tripRepo.GetTripDetailsAsync(tripId);

            if (trip == null)
                return NotFound("Trip not found.");

            if(trip.DepartureTime <= DateTime.Now.AddHours(4))
                return BadRequest(new { Message = "Trip is no longer available." });

            if (trip.DepartureTime <= DateTime.Now)
                return BadRequest(new { Message = "Trip has already departed." });

            var seats = await _tripRepo.GetTripSeatsAsync(tripId);

            return Ok(seats);
        }

        [HttpGet("{tripId}/available-seats")]
        public async Task<IActionResult> GetAvailableSeats(int tripId)
        {
            if (tripId <= 0)
                return BadRequest("Invalid trip ID");

            var trip = await _tripRepo.GetTripDetailsAsync(tripId);

            if (trip == null)
                return NotFound("Trip not found.");
            if(trip.DepartureTime <= DateTime.Now.AddHours(4))
                return BadRequest(new { Message = "Trip is no longer available." });

            if (trip.DepartureTime <= DateTime.Now)
                return BadRequest(new { Message = "Trip has already departed." });

            var seats = await _tripRepo.GetAvailableSeatsAsync(tripId);

            return Ok(seats);
        }

        [HttpPost("select-seat")]
        public async Task<IActionResult> SelectSeat([FromBody] SelectSeatDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);


            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { Message = "Invalid token" });

            var result = await _tripRepo.SelectSeatAsync(dto, int.Parse(userId));
            if (result.BookingId <= 0)
                return BadRequest(result);

            return Ok(result);
        }
    }
}
