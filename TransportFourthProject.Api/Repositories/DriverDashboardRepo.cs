using Microsoft.EntityFrameworkCore;
using System.Data;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs.Employee;

namespace TransportFourthProject.Api.Repositories
{
    public class DriverDashboardRepo : IDriverDashboardRepo
    {
        private readonly AppDbContext _context;
        public DriverDashboardRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DriverTripDto>> GetTodayTripsAsync(int driverId)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var trips = await _context.Trips
                .Where(t =>
                    t.EmployeeId == driverId &&
                    !t.IsDeleted &&
                    t.DepartureTime >= today &&
                    t.DepartureTime < tomorrow)
                .Select(t => new DriverTripDto
                {
                    TripId = t.Id,
                    DepartureTime = t.DepartureTime,
                    ArrivalTime = t.ArrivalTime,

                    StartCity = t.RoutePrice.StartCity.Name,
                    EndCity = t.RoutePrice.EndCity.Name,

                    BusNumber = t.Bus.BusNumber
                })
                .OrderBy(t => t.DepartureTime)
                .ToListAsync();

            return trips;
        }
        public async Task<List<DriverTripDto>> GetTomorrowTripsAsync(int driverId)
        {
            var tomorrow = DateTime.Today.AddDays(1);
            var dayAfter = tomorrow.AddDays(1);

            var trips = await _context.Trips
                .Where(t =>
                    t.EmployeeId == driverId &&
                    !t.IsDeleted &&
                    t.DepartureTime >= tomorrow &&
                    t.DepartureTime < dayAfter)
                .Select(t => new DriverTripDto
                {
                    TripId = t.Id,
                    DepartureTime = t.DepartureTime,
                    ArrivalTime = t.ArrivalTime,
                    StartCity = t.RoutePrice.StartCity.Name,
                    EndCity = t.RoutePrice.EndCity.Name,
                    BusNumber = t.Bus.BusNumber
                })
                .OrderBy(t => t.DepartureTime)
                .ToListAsync();

            return trips;
        }

        public async Task<List<DriverTripDto>> GetAfterTomorrowTripsAsync(int driverId)
        {
            var afterTomorrow = DateTime.Today.AddDays(2);
            var followingDay = afterTomorrow.AddDays(1);

            var trips = await _context.Trips
                .Where(t =>
                    t.EmployeeId == driverId &&
                    !t.IsDeleted &&
                    t.DepartureTime >= afterTomorrow &&
                    t.DepartureTime < followingDay)
                .Select(t => new DriverTripDto
                {
                    TripId = t.Id,
                    DepartureTime = t.DepartureTime,
                    ArrivalTime = t.ArrivalTime,
                    StartCity = t.RoutePrice.StartCity.Name,
                    EndCity = t.RoutePrice.EndCity.Name,
                    BusNumber = t.Bus.BusNumber
                })
                .OrderBy(t => t.DepartureTime)
                .ToListAsync();
            return trips;
        }

        public async Task<int> GetMonthlyTripsCountAsync(int driverId, int year, int month)
        {
            var count = await _context.Trips
                .Where(t =>
                    t.EmployeeId == driverId &&
                    !t.IsDeleted &&
                    t.DepartureTime.Year == year &&
                    t.DepartureTime.Month == month)
                .CountAsync();

            return count;
        }

        public async Task<int> GetYearlyTripsCountAsync(int driverId, int year)
        {
            var count = await _context.Trips
                .Where(t =>
                    t.EmployeeId == driverId &&
                    !t.IsDeleted &&
                    t.DepartureTime.Year == year)
                .CountAsync();

            return count;
        }

    }
}










