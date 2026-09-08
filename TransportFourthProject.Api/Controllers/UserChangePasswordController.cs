using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TransportFourthProject.Api.DTOs.User;
using TransportFourthProject.Api.Models;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Services;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppPolicies.UserOnly)]
    public class UserChangePasswordController : ControllerBase
    {
        private readonly IRepository<User> _userRepo;
        private readonly PasswordHasher _passwordHasher;

        public UserChangePasswordController(IRepository<User> userRepo, PasswordHasher passwordHasher)
        {
            _userRepo = userRepo;
            _passwordHasher = passwordHasher;
        }
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized(new {Message = "Invalid token"});
            }
            var user = await _userRepo.GetByIdAsync(int.Parse(userId));

            if (user == null || !_passwordHasher.VerifyPassword(dto.OldPassword, user.Password))
                return Unauthorized(new { Message = "Invalid credentials" });

            user.Password = _passwordHasher.HashPassword(dto.NewPassword);
            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();

            return Ok(new { Message = "Operation completed successfully" });
        }
    }
}
