using Microsoft.AspNetCore.Mvc;
using TransportFourthProject.Api.Data;
using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.DTOs.TripDiscount;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/admin/tripdiscounts")]
    [ApiController]
    [Authorize(Policy = AppPolicies.ManagerOnly)]
    public class AdminTripDiscountController : ControllerBase
    {
        private readonly IAdminTripDiscountRepository _adminTripDiscountRepo;
        private readonly AppDbContext _context;
        public AdminTripDiscountController(IAdminTripDiscountRepository adminTripDiscountRepo, AppDbContext context)
        {
            _adminTripDiscountRepo = adminTripDiscountRepo;
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddTripDiscount(AddTripDiscountDto dto)
        {
            var result = await _adminTripDiscountRepo.AddTripDiscountAsync(dto);
            return Ok(result);
        }
        [HttpPut("update")]
        public async Task<IActionResult> UpdateTripDiscount(UpdateTripDiscountDto dto)
        {
            var result = await _adminTripDiscountRepo.UpdateTripDiscountAsync(dto);
            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteTripDiscount(int id)
        {
            var result = await _adminTripDiscountRepo.DeleteTripDiscountAsync(id);
            return Ok(result);
        }

        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreTripDiscount(int id)
        {
            var result = await _adminTripDiscountRepo.RestoreTripDiscountAsync(id);
            return Ok(result);
        }

        [HttpGet("deleted-trip-discounts")]
        public async Task<IActionResult> GetDeletedTripDiscounts()
        {
            var result = await _adminTripDiscountRepo.GetDeletedTripDiscountsAsync();
            return Ok(result);
        }
        [HttpGet("active-trip-discounts")]
        public async Task<IActionResult> GetActiveTripDiscounts()
        {
            var result = await _adminTripDiscountRepo.GetActiveTripDiscountsAsync();
            return Ok(result);
        }
    }
}











