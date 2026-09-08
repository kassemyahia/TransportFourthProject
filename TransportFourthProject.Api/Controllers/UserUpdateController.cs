using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TransportFourthProject.Api.DTOs.User;
using TransportFourthProject.Api.Models;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Authorization;
namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppPolicies.UserOnly)]
    public class UserUpdateController : ControllerBase
    {
        private readonly IRepository<User> _userRepo;

        public UserUpdateController(IRepository<User> userRepo)
        {
            _userRepo = userRepo;
        }

        [HttpPatch("update")]
        [Consumes("application/json-patch+json")]
        public async Task<IActionResult> PatchUpdate([FromBody] JsonPatchDocument<UserUpdateProfileDto> patchDoc)
        {
            if (patchDoc == null)
                return BadRequest(new { Message = "Invalid patch document" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { Message = "Invalid token" });

            var user = await _userRepo.GetByIdAsync(int.Parse(userId));
            if (user == null)
                return Unauthorized(new { Message = "User not found" });

            var dto = new UserUpdateProfileDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone
            };

            patchDoc.ApplyTo(dto, ModelState);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            user.FirstName = dto.FirstName ?? user.FirstName;
            user.LastName = dto.LastName ?? user.LastName;
            user.Phone = dto.Phone ?? user.Phone;

            _userRepo.Update(user);
            await _userRepo.SaveChangesAsync();

            return Ok(new { Message = "User updated successfully" });
        }
    }
}
