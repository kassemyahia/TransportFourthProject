using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs.Trip;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/employee/trips")]
    [ApiController]
    [Authorize(Policy = AppPolicies.StaffOrManager)]
    public class EmployeeTripController : ControllerBase
    {
        private readonly IEmployeeTripRepository _employeeTripRepo;
        private readonly AppDbContext _context;

        public EmployeeTripController(IEmployeeTripRepository employeeTripRepo, AppDbContext context)
        {
            _employeeTripRepo = employeeTripRepo;
            _context = context;
        }
        [HttpGet("all-trips")]
        public async Task<IActionResult> GetAllTrips()
        {
            var trips = await _employeeTripRepo.GetAllTripsForEmployeeAsync();
            if (!trips.Any())
                return Ok(new { Message = "No trips available at the moment." });

            var result = trips.Select(t => new EmployeeTripListDto
            {
                TripId = t.Id,
                DepartureTime = t.DepartureTime.ToString("yyyy-MM-dd HH:mm"),
                StartCity = t.RoutePrice.StartCity.Name,
                EndCity = t.RoutePrice.EndCity.Name,
                BusType = t.Bus.BusType.Type,
                BasePrice = (t.RoutePrice.Price.ToString()) + " SYP",
                DiscountName = t.TripDiscount?.Name ?? "no discount",
                DiscountPercentage = (t.TripDiscount?.Percentage ?? 0) + "%",
                IsDeleted = t.IsDeleted
            });

            return Ok(result);
        }

        [HttpGet("details/{tripId}")]
        public async Task<IActionResult> GetTripDetails(int tripId)
        {
            if (tripId <= 0)
                return BadRequest("Invalid trip ID");

            var trip = await _employeeTripRepo.GetTripDetailsForEmployeeAsync(tripId);

            if (trip == null)
                return NotFound("Trip not found");
            
            if (trip.DepartureTime <= DateTime.Now)
                return BadRequest(new { Message = "Trip has already departed." });

            if(trip.RoutePrice.IsDeleted || trip.Bus.BusType.IsDeleted
                || trip.Bus.Status != BusStatus.Active)
                return BadRequest(new { Message = "Trip is no longer available." });

            var bookedSeats = await _employeeTripRepo.GetBookedSeatsForEmployeeAsync(tripId);

            int availableSeats = trip.Bus.BusType.Capacity - bookedSeats;

            string tripStatus = availableSeats == 0 ? "complete" : "available";

            var dto = new EmployeeTripDetailsDto
            {
                TripId = trip.Id,
                DepartureTime = trip.DepartureTime.ToString("yyyy-MM-dd HH:mm"),
                ArrivalTime = trip.ArrivalTime.ToString("yyyy-MM-dd HH:mm"),

                StartCity = trip.RoutePrice.StartCity.Name,
                EndCity = trip.RoutePrice.EndCity.Name,

                BusNumber = trip.Bus.BusNumber,
                BusType = trip.Bus.BusType.Type,

                AvailableSeats = availableSeats,
                TripStatus = tripStatus,

                BasePrice = (trip.RoutePrice.Price.ToString()) + " SYP",

                DriverName = $"{trip.Employee.FirstName} {trip.Employee.LastName}",

                DiscountName = trip.TripDiscount?.Name ?? "no discount",
                DiscountPercentage = (trip.TripDiscount?.Percentage ?? 0) + "%",

                TotalBookings = trip.Bookings.Count(b =>
                b.Status == BookingStatus.Confirmed ||
                b.Status == BookingStatus.PendingPayment),
                IsDeleted = trip.IsDeleted
            };
            return Ok(dto);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTrips(string? startCity, string? endCity, DateTime? date,
                                                    string? busType, string? sortBy, string? order,
                                                    string? status = "all", int? driverId = null,
                                                    int? busId = null, bool? hasDiscount = null,
                                                    string? capacityStatus = null)
        {
            bool hasTime = date.HasValue && date.Value.TimeOfDay != TimeSpan.Zero;
            bool hasMinutes = hasTime && date.Value.Minute != 0;

            var trips = await _employeeTripRepo.SearchTripsForEmployeeAsync(startCity, endCity, date,
                                                        hasTime, hasMinutes, busType,
                                                        sortBy, order,
                                                        status, driverId, busId,
                                                        hasDiscount, capacityStatus);

            if (!trips.Any())
                return Ok(new { Message = "No trips found" });

            var result = trips.Select(t => new EmployeeTripListDto
            {
                TripId = t.Id,
                DepartureTime = t.DepartureTime.ToString("yyyy-MM-dd HH:mm"),
                StartCity = t.RoutePrice.StartCity.Name,
                EndCity = t.RoutePrice.EndCity.Name,
                BusType = t.Bus.BusType.Type,
                BasePrice = (t.RoutePrice.Price.ToString()) + " SYP",
                DiscountName = t.TripDiscount?.Name ?? "no discount",
                DiscountPercentage = (t.TripDiscount?.Percentage ?? 0) + "%",
                IsDeleted = t.IsDeleted
            });
            return Ok(result);
        }

        [HttpGet("search-options")]
        public async Task<IActionResult> GetTripSearchOptions()
        {
            var result = await _employeeTripRepo.GetTripSearchOptionsForEmployeeAsync();
            return Ok(result);
        }

        [HttpGet("drivers/available")]
        public async Task<IActionResult> GetAvailableDrivers(DateTime departureTime)
        {
            var drivers = await _employeeTripRepo.GetAvailableDriversAsync(departureTime);

            var result = drivers.Select(d => new AvailableDriverForAddTripForEmployeeDto
            {
                DriverId = d.Id,
                FullName = d.FirstName + " " + d.LastName
            });

            return Ok(result);
        }

        [HttpGet("buses/available")]
        public async Task<IActionResult> GetAvailableBuses(DateTime departureTime, int busTypeId)
        {
            var buses = await _employeeTripRepo.GetAvailableBusesAsync(departureTime, busTypeId);

            var result = buses.Select(b => new AvailableBusesForAddTripForEmployeeDto
            {
                BusId = b.Id,
                BusNumber = b.BusNumber,
                BusType = b.BusType.Type,
                Capacity = b.BusType.Capacity
            });

            return Ok(result);
        }

        [HttpGet("discounts")]
        public async Task<IActionResult> GetAvailableDiscounts()
        {
            var discounts = await _employeeTripRepo.GetAvailableTripDiscountsAsync();

            var result = discounts.Select(d => new AvailableDiscountForAddTripForEmployeeDto
            {
                DiscountId = d.Id,
                Name = d.Name,
                Percentage = d.Percentage
            });
            return Ok(result);
        }

        [HttpGet("route-prices")]
        public async Task<IActionResult> GetRoutePrices()
        {
            var routes = await _employeeTripRepo.GetRoutePricesAsync();

            var result = routes.Select(r => new RoutePriceForEmployeeDto
            {
                RoutePriceId = r.Id,
                StartCity = r.StartCity.Name,
                EndCity = r.EndCity.Name,
                Price = r.Price,
                BusType = r.BusType.Type,
                DurationHours = r.DurationHours
            });

            return Ok(result);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateTrip(EmployeeAddTripDto dto)
        {
            try
            {
                var trip = await _employeeTripRepo.AddTripForEmployeeAsync(dto);

                return Ok(new
                {
                    TripId = trip.Id,
                    Message = "Trip created successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Error = ex.Message
                });
            }
        }

        [HttpPatch("update")]
        public async Task<IActionResult> PatchTrip([FromBody] EmployeePatchTripDto dto)
        {
            var result = await _employeeTripRepo.PatchTripForEmployeeAsync(dto);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    errorField = result.ErrorField,
                    message = result.Message,
                    suggestion = result.Suggestion
                });
            }
            return Ok(new
            {
                message = result.Message
            });
        }

        [HttpGet("edit-data/{tripId}")]
        public async Task<IActionResult> GetTripEditData(int tripId)
        {
            var result = await _employeeTripRepo.GetTripEditDataAsync(tripId);

            if (result == null)
                return NotFound(new
                {
                    message = "Trip not found"
                });

            return Ok(result);
        }

        [HttpPut("delete/{tripId}")]
        public async Task<IActionResult> DeleteTrip(int tripId)
        {
            var result = await _employeeTripRepo.DeleteTripAsync(tripId);

            if (!result)
            {
                return BadRequest(new
                {
                    Message = "Cannot delete this trip."
                });
            }

            return Ok(new
            {
                Message = "Trip deleted successfully",
                TripId = tripId,
            });
        }

        [HttpGet("{tripId}/seats-with-users")]
        public async Task<IActionResult> GetTripSeatsWithUsers(int tripId)
        {
            if (tripId <= 0)
                return BadRequest("Invalid trip ID");

            var seats = await _employeeTripRepo.GetTripSeatsWithUsersAsync(tripId);

            if (seats == null)
                return NotFound("Trip not found or has been deleted.");

            return Ok(seats);
        }

        [HttpGet("{tripId}/seat-summary")]
        public async Task<IActionResult> GetTripSeatStatusSummaryAsync(int tripId)
        {
            if (tripId <= 0)
                return BadRequest("Invalid trip ID");

            var summary = await _employeeTripRepo.GetTripSeatStatusSummaryAsync(tripId);

            if (summary == null)
                return NotFound("Trip not found or deleted.");

            return Ok(summary);
        }




        [HttpGet("trips/count/month")]
        public async Task<IActionResult> GetTripsCountInMonth(int month, int year)
        {
            try
            {
                var count = await _employeeTripRepo.GetTripsCountInMonthAsync(month, year);
                return Ok(new { month, year, count });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("trips/count/year")]
        public async Task<IActionResult> GetTripsCountInYear(int year)
        {
            try
            {
                var count = await _employeeTripRepo.GetTripsCountInYearAsync(year);
                return Ok(new { year, count });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("trips/count/route-price")]
        public async Task<IActionResult> GetTripsCountByRoutePrice()
        {
            var result = await _employeeTripRepo.GetTripsCountByRoutePriceAsync();
            return Ok(result);
        }

        [HttpGet("trips/count/bus-type")]
        public async Task<IActionResult> GetTripsCountByBusType()
        {
            var result = await _employeeTripRepo.GetTripsCountByBusTypeAsync();
            return Ok(result);
        }

        [HttpGet("trips/on-roads")]
        public async Task<IActionResult> GetTripsOnRoad()
        {
            var result = await _employeeTripRepo.GetTripsOnRoadsAsync();
            return Ok(result);
        }

        [HttpGet("trips/upcoming")]
        public async Task<IActionResult> GetUpcomingTrips()
        {
            var result = await _employeeTripRepo.GetUpcomingTripsAsync();
            return Ok(result);
        }

        [HttpGet("trips/today")]
        public async Task<IActionResult> GetTodayTrips()
        {
            var result = await _employeeTripRepo.GetTodayTripsAsync();
            return Ok(result);
        }

        [HttpGet("trips/tomorrow")]
        public async Task<IActionResult> GetTomorrowTrips()
        {
            var result = await _employeeTripRepo.GetTomorrowTripsAsync();
            return Ok(result);
        }



    }
}
