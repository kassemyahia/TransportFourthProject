using Microsoft.EntityFrameworkCore;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs.Payment;
using TransportFourthProject.Api.DTOs.Pricing;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Models;
using TransportFourthProject.Api.Models.Payments;
using TransportFourthProject.Api.Services.Pricing;

namespace TransportFourthProject.Api.Services.Payments
{
    public class PaymentService
    {
        private readonly AppDbContext _context;
        private readonly FakePaymentGateway _gateway;

        public PaymentService(AppDbContext context, FakePaymentGateway gateway)
        {
            _context = context;
            _gateway = gateway;
        }

        public async Task<PaymentResponseDto> ProcessPaymentAsync(
            int bookingId,
            PaymentMethod method,
            int userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.Payment)
                .Include(b => b.Trip).ThenInclude(t => t.RoutePrice)
                .Include(b => b.Trip).ThenInclude(t => t.TripDiscount)
                .Include(b => b.User).ThenInclude(u => u.UserDiscountTickets)
                                     .ThenInclude(ud => ud.UserDiscount)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
                return new PaymentResponseDto
                {
                    PaymentStatus = "NotFound",
                    Message = "Booking not found"
                };

            if (booking.Status == BookingStatus.Cancelled ||
                booking.Status == BookingStatus.Expired ||
                (booking.Status == BookingStatus.PendingPayment &&
                 booking.ExpirationTime < DateTime.Now))
            {
                return new PaymentResponseDto
                {
                    BookingId = booking.Id,
                    PaymentStatus = "Error",
                    Message = "Booking is cancelled or expired"
                };
            }

            var seatIsUnavailable = await _context.Bookings.AnyAsync(other =>
                other.Id != booking.Id &&
                other.TripId == booking.TripId &&
                other.SeatNumber == booking.SeatNumber &&
                (other.Status == BookingStatus.PendingPayment ||
                 other.Status == BookingStatus.Confirmed));
            if (seatIsUnavailable)
            {
                return new PaymentResponseDto
                {
                    BookingId = booking.Id,
                    PaymentStatus = "Error",
                    Message = "This seat is no longer available"
                };
            }

            string idempotencyKey = Guid.NewGuid().ToString();

            if (booking.Payment == null)
            {
                var priceService = new PriceCalculatorService(_context);
                var priceResult = await priceService.CalculateFinalPriceAsync(
                    new PriceRequestDto { BookingId = booking.Id });

                if (priceResult == null)
                    return new PaymentResponseDto
                    {
                        PaymentStatus = "Error",
                        Message = "Could not calculate price"
                    };

                var newPayment = new Payment
                {
                    BookingId = booking.Id,
                    Status = PaymentStatus.Pending,
                    Amount = priceResult.FinalPrice,
                    PaymentDate = DateTime.Now,
                    IdempotencyKey = idempotencyKey
                };

                newPayment.PaymentMethod = method.ToString();
                _context.Payments.Add(newPayment);
                await _context.SaveChangesAsync();

                booking.Payment = newPayment;
                booking.PaymentId = newPayment.Id;
                booking.FinalPrice = priceResult.FinalPrice;

                await _context.SaveChangesAsync();
            }

            var payment = booking.Payment;

            var existingAttempt = await _context.PaymentAttempts
                .FirstOrDefaultAsync(pa => pa.IdempotencyKey == idempotencyKey);

            if (existingAttempt != null)
            {
                if (existingAttempt.Status == PaymentAttemptStatus.Successful)
                    return new PaymentResponseDto
                    {
                        PaymentId = payment.Id,
                        BookingId = booking.Id,
                        Amount = payment.Amount,
                        PaymentStatus = "Success",
                        Message = "Payment already processed"
                    };

                return new PaymentResponseDto
                {
                    PaymentId = payment.Id,
                    BookingId = booking.Id,
                    Amount = payment.Amount,
                    PaymentStatus = "Failed",
                    Message = "Payment failed previously"
                };
            }

            if (payment.Status == PaymentStatus.Successful)
            {
                return new PaymentResponseDto
                {
                    PaymentId = payment.Id,
                    BookingId = booking.Id,
                    Amount = payment.Amount,
                    PaymentStatus = "Success",
                    Message = "Booking already paid"
                };
            }

            var attempt = new PaymentAttempt
            {
                PaymentId = payment.Id,
                IdempotencyKey = idempotencyKey,
                Status = PaymentAttemptStatus.Processing,
                Amount = payment.Amount,
                CreatedAt = DateTime.Now
            };

            _context.PaymentAttempts.Add(attempt);
            await _context.SaveChangesAsync();

            var result = await _gateway.ProcessAsync(payment, payment.Amount, idempotencyKey);

            attempt.Status = result.IsSuccess
                ? PaymentAttemptStatus.Successful
                : PaymentAttemptStatus.Failed;

            attempt.TransactionReference = result.TransactionReference;
            attempt.CompletedAt = DateTime.Now;
            attempt.ErrorMessage = result.ErrorMessage;

            await _context.SaveChangesAsync();

            if (result.IsSuccess)
            {
                payment.Status = PaymentStatus.Successful;
                payment.PaymentDate = DateTime.Now;
                payment.TransactionReference = result.TransactionReference;

                booking.Status = BookingStatus.Confirmed;
                booking.SeatStatus = SeatStatus.Confirmed;
                booking.ExpirationTime = null;

                await _context.SaveChangesAsync();
                return new PaymentResponseDto
                {
                    PaymentId = payment.Id,
                    BookingId = booking.Id,
                    Amount = payment.Amount,
                    PaymentStatus = "Success",
                    Message = "Payment completed successfully"
                };
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                await _context.SaveChangesAsync();

                return new PaymentResponseDto
                {
                    PaymentId = payment.Id,
                    BookingId = booking.Id,
                    Amount = payment.Amount,
                    PaymentStatus = "Failed",
                    Message = "Payment failed"
                };
            }
        }
    }
}
