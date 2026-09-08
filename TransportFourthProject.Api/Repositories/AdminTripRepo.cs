using Microsoft.EntityFrameworkCore;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs.Trip;
using TransportFourthProject.Api.Models;

namespace TransportFourthProject.Api.Repositories
{
    public class AdminTripRepo : Repository<Trip>, IAdminTripRepo
    {
        private readonly AppDbContext _context;

        public AdminTripRepo(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<List<TripConfirmedPassengersDto>> GetConfirmedPassengersAsync(int tripId)
        {
            var passengers = await _context.Bookings
                .Where(b => b.TripId == tripId && b.Status == Enums.BookingStatus.Confirmed)
                .Include(b => b.User)
                .Select(b => new TripConfirmedPassengersDto
                {
                    TripId = b.TripId,
                    SeatNumber = b.SeatNumber,
                    FullName = b.User.FirstName + " " + b.User.LastName
                })
                .ToListAsync();

            return passengers;
        }
    }
}
