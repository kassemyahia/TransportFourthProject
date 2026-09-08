using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs.Booking;
using TransportFourthProject.Api.DTOs.Pricing;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Models;
using TransportFourthProject.Api.Services.Pricing;
using System.Security.Claims;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppPolicies.UserOnly)]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PriceCalculatorService _priceService;

        public BookingController(AppDbContext context, PriceCalculatorService priceService)
        {
            _context = context;
            _priceService = priceService;
        }
        [HttpPost("cancel-booking/{bookingId}")]
        public async Task<IActionResult> CancelBooking(int bookingId)
        {
            if (bookingId <= 0)
                return BadRequest(new BookingResponseDto
                {
                    BookingId = bookingId,
                    BookingStatus = "Invalid",
                    Message = "Invalid booking Id"
                });

            var userId = CurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return NotFound(new BookingResponseDto
                {
                    BookingId = bookingId,
                    BookingStatus = "NotFound",
                    Message = "Booking not found"
                });

            if (booking.Trip.DepartureTime <= DateTime.Now)
                return BadRequest(new BookingResponseDto
                {
                    BookingId = bookingId,
                    SeatNumber = booking.SeatNumber,
                    BookingStatus = "Expired",
                    Message = "Trip has already departed."
                });
            if (booking.Status == BookingStatus.PendingPayment)
            {
                if (booking.ExpirationTime.HasValue && booking.ExpirationTime < DateTime.Now)
                    return BadRequest(new BookingResponseDto
                    {
                        BookingId = booking.Id,
                        SeatNumber = booking.SeatNumber,
                        BookingStatus = "Expired",
                        Message = "Booking already expired"
                    });

                booking.Status = BookingStatus.Cancelled;
                booking.SeatStatus = SeatStatus.Available;
                booking.ExpirationTime = null;

                await _context.SaveChangesAsync();

                return Ok(new BookingResponseDto    
                {
                    BookingId = booking.Id,
                    SeatNumber = booking.SeatNumber,
                    BookingStatus = booking.Status.ToString(),
                    Message = "Temporary booking cancelled successfully"
                });
            }

            if (booking.Status == BookingStatus.Confirmed)
            {
                return Ok(new BookingResponseDto
                {
                    BookingId = booking.Id,
                    SeatNumber = booking.SeatNumber,
                    BookingStatus = booking.Status.ToString(),
                    Message = "A 25% deduction will be applied to the booking amount. Are you sure you want to proceed?"
                });
            }

            return BadRequest(new BookingResponseDto
            {
                BookingId = booking.Id,
                SeatNumber = booking.SeatNumber,
                BookingStatus = booking.Status.ToString(),
                Message = "Only temporary or confirmed bookings can be cancelled"
            });
        }

        [HttpPost("confirm-cancel-booking/{bookingId}")]
        public async Task<IActionResult> ConfirmCancelBooking(int bookingId)
        {
            if (bookingId <= 0)
                return BadRequest(new BookingResponseDto
                {
                    BookingId = bookingId,
                    BookingStatus = "Invalid",
                    Message = "Invalid booking Id"
                });

            var userId = CurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return NotFound(new BookingResponseDto
                {
                    BookingId = bookingId,
                    BookingStatus = "NotFound",
                    Message = "Booking not found"
                });

            if (booking.Trip.DepartureTime <= DateTime.Now)
                return BadRequest(new BookingResponseDto
                {
                    BookingId = booking.Id,
                    SeatNumber = booking.SeatNumber,
                    BookingStatus = "Expired",
                    Message = "Trip has already departed."
                });

            if (booking.Status != BookingStatus.Confirmed)
            {
                return BadRequest(new BookingResponseDto
                {
                    BookingId = booking.Id,
                    SeatNumber = booking.SeatNumber,
                    BookingStatus = booking.Status.ToString(),
                    Message = "Only confirmed bookings can be cancelled"
                });
            }


            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
            if (payment != null && payment.Status == PaymentStatus.Successful)
            {
                decimal newAmount = payment.Amount * 0.75m;
                payment.Amount = newAmount;
                payment.Status = PaymentStatus.PartioallyRefunded;
                payment.PaymentDate = DateTime.Now;
            }


            booking.Status = BookingStatus.Cancelled;
            booking.SeatStatus = SeatStatus.Available;
            booking.ExpirationTime = null;

            await _context.SaveChangesAsync();

            return Ok(new BookingResponseDto
            {
                BookingId = booking.Id,
                SeatNumber = booking.SeatNumber,
                BookingStatus = booking.Status.ToString(),
                Message = "Confirmed booking cancelled successfully.A 25% deduction  has been applied to the booking amount."
            });
        }
        
        [HttpPost("temporary-booking/{bookingId}")]
        public async Task<IActionResult> TemporaryBooking(int bookingId)
        {
            if (bookingId <= 0)
            {
                return BadRequest(new BookingResponseDto
                {
                    BookingId = bookingId,
                    BookingStatus = "Invalid",
                    Message = "Invalid booking id"
                });
            }

            var userId = CurrentUserId();
            var booking = await _context.Bookings
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
            if (booking == null)
            {
                return NotFound(new BookingResponseDto
                {
                    BookingId = bookingId,
                    BookingStatus = "NotFound",
                    Message = "Booking not found"
                });
            }

            if (booking.Trip.DepartureTime <= DateTime.Now)
            {
                return BadRequest(new BookingResponseDto
                {
                    BookingId = bookingId,
                    BookingStatus = "Expired",
                    Message = "Trip has already departed."
                });
            }

            if (booking.Status != BookingStatus.Created)
            {
                return BadRequest( new BookingResponseDto
                {
                    BookingId = booking.Id,
                    SeatNumber = booking.SeatNumber,
                    BookingStatus = booking.Status.ToString(),
                    Message = "Only a newly created booking can be temporarily reserved."
                });
            }

            var seatIsUnavailable = await _context.Bookings.AnyAsync(other =>
                other.Id != booking.Id &&
                other.TripId == booking.TripId &&
                other.SeatNumber == booking.SeatNumber &&
                (other.Status == BookingStatus.PendingPayment ||
                 other.Status == BookingStatus.Confirmed));
            if (seatIsUnavailable)
            {
                return Conflict(new BookingResponseDto
                {
                    BookingId = booking.Id,
                    SeatNumber = booking.SeatNumber,
                    BookingStatus = booking.Status.ToString(),
                    Message = "This seat is no longer available."
                });
            }

            booking.Status = BookingStatus.PendingPayment;
            booking.SeatStatus = SeatStatus.Reserved;
            booking.ExpirationTime = DateTime.Now.AddHours(2);

            await _context.SaveChangesAsync();

            var response = new BookingResponseDto
            {
                BookingId = booking.Id,
                SeatNumber = booking.SeatNumber,
                ExpirationTime = booking.ExpirationTime,
                BookingStatus = booking.Status.ToString(),
                Message = "Temporary booking started. Seat reserved until expiration."
            };
            return Ok(response);
        }

        private int CurrentUserId()
        {
            return int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        }

    }
}
