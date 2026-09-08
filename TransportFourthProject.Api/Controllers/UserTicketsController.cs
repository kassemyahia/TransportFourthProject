using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs.Pricing;
using TransportFourthProject.Api.DTOs.User;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Services.Pricing;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Authorize(Policy = AppPolicies.UserOnly)]
    [ApiController]
    [Route("api/[controller]")]
    public class UserTicketsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PriceCalculatorService _priceCalculatorService;

        public UserTicketsController(AppDbContext context, PriceCalculatorService priceCalculatorService)
        {
            _context = context;
            _priceCalculatorService = priceCalculatorService;
        }

        [HttpGet("my-tickets")]
        public async Task<IActionResult> GetUserTickets()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { Message = "Invalid token" });
            }
            var bookings = await _context.Bookings
                .Include(b => b.Trip)
                    .ThenInclude(t => t.RoutePrice)
                    .ThenInclude(rp => rp.StartCity)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.RoutePrice)
                    .ThenInclude(rp => rp.EndCity)
                .Include(b => b.Trip)
                    .ThenInclude(t => t.Bus)
                        .ThenInclude(bus => bus.BusType)
                .Include(b => b.User)
                .Where(b => b.UserId == int.Parse(userId) && 
                (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.PendingPayment)
                && b.Trip.DepartureTime.AddHours(2) > DateTime.Now)
                .ToListAsync();
            if (!bookings.Any())
            {
                return Ok(new {Message = "No tickets found." });
            }
            var tickets = new List<GetAllUserTicketsDto>();

            foreach (var booking in bookings)
            {
                var priceResult = await _priceCalculatorService.CalculateFinalPriceAsync(new PriceRequestDto { BookingId = booking.Id });
                booking.FinalPrice = priceResult?.FinalPrice ?? 0;

                tickets.Add(new GetAllUserTicketsDto
                {
                    FirstName = booking.User.FirstName,
                    LastName = booking.User.LastName,
                    BookingId = booking.Id,
                    TripId = booking.TripId,
                    TripDate = booking.Trip.DepartureTime.ToString("yyyy-MM-dd:HH:mm"),
                    StartCity = booking.Trip.RoutePrice.StartCity.Name,
                    EndCity = booking.Trip.RoutePrice.EndCity.Name,
                    BusType = booking.Trip.Bus.BusType.Type,
                    BusNumber = booking.Trip.Bus.BusNumber,
                    SeatNumber = booking.SeatNumber,
                    Status = booking.Status.ToString(),
                    ExpirationTime = booking.Status == BookingStatus.PendingPayment
                                     ? booking.ExpirationTime
                                     : null,
                    FinalPrice = (priceResult?.FinalPrice ?? 0).ToString() + " SYP"
                });
            }

            return Ok(tickets);
        }
        [HttpGet("my-discount-tickets")]
        public async Task<IActionResult> GetUserActiveDiscountTickets()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new { Message = "Invalid token" });
            }
            var discounts = await _context.UserDiscountTickets
                .Include(d => d.UserDiscount)
                .Include(d => d.User)
                .Where(d => d.UserId == int.Parse(userId) && d.EndDate > DateTime.Now)
                .Select(d => new GetAllUserDiscountTicketsDto
                {
                    FirstName = d.User.FirstName,
                    LastName = d.User.LastName,
                    DiscountTicketNumber = d.Id,
                    DiscountName = d.UserDiscount.Name,
                    Percentage = d.UserDiscount.Percentage + "%",
                    StartDate = d.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = d.EndDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();
            if(!discounts.Any())
            {
                return Ok(new { Message = "No active discount tickets found." });
            }
            return Ok(discounts);
        }
    }
}
