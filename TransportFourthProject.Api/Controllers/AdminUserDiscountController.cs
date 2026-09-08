using Microsoft.AspNetCore.Mvc;
using TransportFourthProject.Api.Data;
using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.DTOs.UserDiscount;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/admin/userdiscounts")]
    [ApiController]
    [Authorize(Policy = AppPolicies.ManagerOnly)]
    public class AdminUserDiscountController : ControllerBase
    {
        private readonly IAdminUserDiscountRepository _adminUserDiscountRepo;
        private readonly AppDbContext _context;
        public AdminUserDiscountController(IAdminUserDiscountRepository adminUserDiscountRepo, AppDbContext context)
        {
            _adminUserDiscountRepo = adminUserDiscountRepo;
            _context = context;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUserDiscount(AddUserDiscountDto dto)
        {
            var result = await _adminUserDiscountRepo.AddUserDiscountAsync(dto);
            return Ok(result);
        }
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUserDiscount(UpdateUserDiscountDto dto)
        {
            var result = await _adminUserDiscountRepo.UpdateUserDiscountAsync(dto);
            return Ok(result);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteUserDiscount(int id)
        {
            var result = await _adminUserDiscountRepo.DeleteUserDiscountAsync(id);
            return Ok(result);
        }

        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreUserDiscount(int id)
        {
            var result = await _adminUserDiscountRepo.RestoreUserDiscountAsync(id);
            return Ok(result);
        }
        [HttpGet("deleted-user-discounts")]
        public async Task<IActionResult> GetDeletedUserDiscounts()
        {
            var result = await _adminUserDiscountRepo.GetDeletedUserDiscountsAsync();
            return Ok(result);
        }
        [HttpGet("active-user-discounts")]  
        public async Task<IActionResult> GetActiveUserDiscounts()
        {
            var result = await _adminUserDiscountRepo.GetActiveUserDiscountsAsync();
            return Ok(result);
        }
    }
}
