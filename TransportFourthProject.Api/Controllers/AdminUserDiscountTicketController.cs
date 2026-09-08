using Microsoft.AspNetCore.Mvc;
using TransportFourthProject.Api.Data;
using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.DTOs.UserDiscountTicket;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [Route("api/admin/user-discount-ticket")]
    [ApiController]
    [Authorize(Policy = AppPolicies.ManagerOnly)]
    public class AdminUserDiscountTicketController : ControllerBase
    {
        private readonly IAdminUserDiscountTicketRepo _adminUserDiscountTicketRepo;
        private readonly AppDbContext _context;
        public AdminUserDiscountTicketController(IAdminUserDiscountTicketRepo adminUserDiscountTicketRepo, AppDbContext context)
        {
            _adminUserDiscountTicketRepo = adminUserDiscountTicketRepo;
            _context = context;
        }
        [HttpPost("add")]
        public async Task<IActionResult> AddUserDiscountTicket(AddUserDiscountTicketDto dto)
        {
            var result = await _adminUserDiscountTicketRepo.AddUserDiscountTicketAsync(dto);
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserDiscountTickets(int userId)
        {
            var result = await _adminUserDiscountTicketRepo.GetUserDiscountTicketsAsync(userId);
            return Ok(result);
        }
    }
}
