using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TransportFourthProject.Api.DTOs.Employee;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Models;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Services;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/all-op-on-employee-table")]
    [Authorize(Policy = AppPolicies.ManagerOnly)]
    public class AdminAllOperationOnEmployeeTableController : ControllerBase
    {
        private readonly IAdminAllOperationOnEmployeeTableRepo _repo;
        private readonly PasswordHasher _passwordHasher;
        private readonly AesEncryptionService _aesEncryptionService;
        public AdminAllOperationOnEmployeeTableController(IAdminAllOperationOnEmployeeTableRepo repo, 
            PasswordHasher passwordHasher, AesEncryptionService aesEncryptionService)
        {
            _repo = repo;
            _passwordHasher = passwordHasher;
            _aesEncryptionService = aesEncryptionService;

        }

        [HttpPost("add")]
        public async Task<IActionResult> AddEmployee([FromBody] AddEmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var employee = new Employee
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Password = _passwordHasher.HashPassword(dto.Password),
                NationalNumber = _aesEncryptionService.Encrypt(dto.NationalNumber),
                Salary = dto.Salary,
                Role = dto.Role,
                Status = EmployeeStatus.Active,

                HireDate = DateTime.Now,

                LicenseNumber = dto.LicenseNumber
            };

            await _repo.AddAsync(employee);
            await _repo.SaveChangesAsync();

            return Ok(new
            {
                Message = "Employee added successfully",
                EmployeeId = employee.Id
            });
        }

        [HttpPatch("update/{id}")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeDto dto)
        {
            var employee = await _repo.GetByIdAsync(id);
            if (employee == null)
                return NotFound(new { Message = "Employee not found" });

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                employee.FirstName = dto.FirstName;

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                employee.LastName = dto.LastName;

            if (!string.IsNullOrWhiteSpace(dto.Phone))
                employee.Phone = dto.Phone;

            if (dto.Salary.HasValue)
                employee.Salary = dto.Salary.Value;

            _repo.Update(employee);
            await _repo.SaveChangesAsync();

            return Ok(new { Message = "Employee updated successfully" });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteEmployee(int id, [FromQuery] EmployeeStatus status)
        {
            var employee = await _repo.GetByIdAsync(id);
            if (employee == null)
                return NotFound(new { Message = "Employee not found" });

            // تغيير الحالة فقط
            employee.Status = status;

            _repo.Update(employee);
            await _repo.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Employee status changed to {status}"
            });
        }

        [HttpGet("status-list")]
        public IActionResult GetEmployeeStatusList()
        {
            var statuses = Enum.GetValues(typeof(EmployeeStatus))
                .Cast<EmployeeStatus>()
                .Select(s => new
                {
                    Id = (int)s,
                    Name = s.ToString()
                })
                .ToList();

            return Ok(statuses);
        }
    }
}
