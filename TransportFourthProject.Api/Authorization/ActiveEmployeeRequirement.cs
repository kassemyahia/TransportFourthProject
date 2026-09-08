using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.Enums;

namespace TransportFourthProject.Api.Authorization
{
    public sealed class ActiveEmployeeRequirement : IAuthorizationRequirement
    {
    }

    public sealed class ActiveEmployeeHandler : AuthorizationHandler<ActiveEmployeeRequirement>
    {
        private readonly AppDbContext _context;

        public ActiveEmployeeHandler(AppDbContext context)
        {
            _context = context;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ActiveEmployeeRequirement requirement)
        {
            var idValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var roleValue = context.User.FindFirstValue(ClaimTypes.Role);
            if (context.User.FindFirstValue("AccountType") != "Employee" ||
                !int.TryParse(idValue, out var employeeId) ||
                !Enum.TryParse<EmployeeRole>(roleValue, out var role))
            {
                return;
            }

            var isActive = await _context.Employees.AnyAsync(employee =>
                employee.Id == employeeId &&
                employee.Status == EmployeeStatus.Active &&
                employee.Role == role);

            if (isActive)
            {
                context.Succeed(requirement);
            }
        }
    }
}
