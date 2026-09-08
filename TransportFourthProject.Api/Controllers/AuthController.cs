using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TransportFourthProject.Api.Data;
using TransportFourthProject.Api.DTOs;
using TransportFourthProject.Api.DTOs.Employee;
using TransportFourthProject.Api.DTOs.User;
using TransportFourthProject.Api.DTOs.User.ForgotPassword;
using TransportFourthProject.Api.Enums;
using TransportFourthProject.Api.Models;
using TransportFourthProject.Api.Repositories;
using TransportFourthProject.Api.Services;
using TransportFourthProject.Api.Settings;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IRepository<User> _userRepo;
        private readonly IRepository<RefreshToken> _refreshTokenRepo;
        private readonly TokenService _tokenService;
        private readonly JwtSettings _jwt;
        private readonly PasswordHasher _passwordHasher;
        private readonly AesEncryptionService _aesEncryptionService;
        public AuthController(AppDbContext context, IRepository<User> userRepo, IRepository<RefreshToken> refreshTokenRepo,
            TokenService tokenService, AesEncryptionService aesEncryptionService,
            PasswordHasher passwordHasher, JwtSettings jwt)
        {
            _context = context;
            _userRepo = userRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _aesEncryptionService = aesEncryptionService;
            _jwt = jwt;
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {

            if(!Regex.IsMatch(dto.Phone, @"^09\d{8}$"))
                return BadRequest(new { Message = "Phone number must start with 09 and be 10 digits" });

            var user = (await _userRepo.GetAsync(u => u.Phone == dto.Phone));
            if (user == null)
                return Unauthorized(new { Message = "Login failed" });

            var passwordCheck = _passwordHasher.VerifyPassword(dto.Password, user.Password);
            if (!passwordCheck)
                return Unauthorized(new { Message = "Login failed" });

            var existingRefresh = await _refreshTokenRepo.GetAsync(r =>
                r.UserId == user.Id && r.ExpiresAt > DateTime.Now && !r.IsRevoked);
            if(existingRefresh != null)
            {
                return Ok(new
                {
                    Message = "You are already logged in",
                    AccessToken = _tokenService.GenerateUserToken(user),
                    RefreshToken = existingRefresh.Token,
                    User = new
                    {
                        user.Id,
                        user.FirstName,
                        user.LastName,
                        user.Phone,
                    }
                });
            }

            var accessToken = _tokenService.GenerateUserToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refresh = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.Now.AddDays(7),
            };
            
            await _refreshTokenRepo.AddAsync(refresh);
            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new
            {
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Phone,
                }
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            if(!Regex.IsMatch(dto.FirstName, @"^[a-zA-Z]+$"))
                return BadRequest(new { Message = "First name must contain only letters" });

            if (!Regex.IsMatch(dto.LastName, @"^[a-zA-Z]+$"))
                return BadRequest(new { Message = "Last name must contain only letters" });

            if (!Regex.IsMatch(dto.Phone, @"^09\d{8}$"))
                return BadRequest(new { Message = "Phone number must start with 09 and be 10 digits" });

            if (!Regex.IsMatch(dto.NationalNumber, @"^\d{11}$"))
                return BadRequest(new { Message = "National number must be 11 digits" });

            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { Message = "Registration failed" });

            var phoneExists = await _userRepo.ExistsAsync(u => u.Phone == dto.Phone);
            if (phoneExists)
                return BadRequest(new { Message = "Registration failed" });

            var encryptedNationalNumber = _aesEncryptionService.Encrypt(dto.NationalNumber);
            var nationalExists = await _userRepo.ExistsAsync(u =>
                u.NationalNumber == encryptedNationalNumber);
            if (nationalExists)
                return BadRequest(new { Message = "Registration failed" });

            var hashedPassword = _passwordHasher.HashPassword(dto.Password);
            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                NationalNumber = encryptedNationalNumber,
                Password = hashedPassword
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            var accessToken = _tokenService.GenerateUserToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refresh = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.Now.AddDays(7),
                IsRevoked = false
            };

            await _refreshTokenRepo.AddAsync(refresh);
            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new
            {
                Message = "Registered successfully",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Phone,
                }
            });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var storedToken = await _refreshTokenRepo.GetAsync(t => t.Token == dto.RefreshToken);

            if (storedToken == null
                || storedToken.ExpiresAt < DateTime.Now
                || storedToken.IsRevoked)
            {
                return BadRequest(new { Message = "Refresh failed" });
            }

            object person = null;

            if (storedToken.UserId != null)
            {
                var user = await _userRepo.GetAsync(u => u.Id == storedToken.UserId);
                if (user == null)
                    return BadRequest(new { Message = "Refresh failed" });

                person = user;
            }

            else if (storedToken.EmployeeId != null)
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Id == storedToken.EmployeeId);

                if (employee == null || employee.Status != EmployeeStatus.Active)
                    return BadRequest(new { Message = "Refresh failed" });

                person = employee;
            }
            else
            {
                return BadRequest(new { Message = "Refresh failed" });
            }

            var newAccessToken = _tokenService.GenerateUserToken(person);

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            storedToken.IsRevoked = true;
            _refreshTokenRepo.Update(storedToken);

            var newToken = new RefreshToken
            {
                Token = newRefreshToken,
                ExpiresAt = DateTime.Now.AddDays(7),
                IsRevoked = false,
                UserId = storedToken.UserId,
                EmployeeId = storedToken.EmployeeId
            };

            await _refreshTokenRepo.AddAsync(newToken);
            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new
            {
                Message = "Token refreshed",
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _jwt.DurationInMinutes * 60
            });
        }
        [Authorize(Policy = AppPolicies.UserOnly)]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { Message = "Invalid token" });

            var user = await _userRepo.GetByIdAsync(int.Parse(userId));
            if (user == null)
                return Unauthorized(new { Message = "User not found" });

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Phone,
            });
        }

        [Authorize(Policy = AppPolicies.UserOnly)]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { Message = "Invalid token" });

            var tokens = await _refreshTokenRepo.FindAsync(r => r.UserId == int.Parse(userId)
                                                           && !r.IsRevoked);

            if(tokens == null || !tokens.Any())
                return Ok(new { Message = "You are already logged out" });

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                _refreshTokenRepo.Update(token);
            }

            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new { Message = "Logged out successfully" });
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Phone == dto.PhoneNumber);
            if (user == null)
                return BadRequest(new { Message = "Phone number not found" });

            var random = new Random();
            bool success = random.Next(0, 2) == 1;

            if (!success)
                return BadRequest(new { Message = "Failed to send verification code. Try again." });

            var code = random.Next(1000, 9999).ToString();

            user.ResetCode = code;
            user.ResetCodeExpiry = DateTime.Now.AddMinutes(5);

            _context.SaveChanges();

            return Ok(new 
            {
                Message = "Verification code sent successfully",
                Code = code
            });
        }

        [HttpPost("verify-reset-code")]
        public IActionResult VerifyResetCode([FromBody] VerifyResetCodeDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Phone == dto.PhoneNumber);
            if (user == null)
                return BadRequest(new { Message = "Phone number not found" });

            if (string.IsNullOrEmpty(user.ResetCode))
                return BadRequest(new { Message = "No reset code found. Please request a new one." });

            if (!Regex.IsMatch(dto.Code, @"^\d{4}$"))
                return BadRequest(new { Message = "Code must be exactly 4 digits." });

            if (user.ResetCode != dto.Code)
                return BadRequest(new { Message = "Invalid verification code" });

            if (user.ResetCodeExpiry == null || user.ResetCodeExpiry < DateTime.Now)
                return BadRequest(new { Message = "Verification code expired" });

            return Ok(new { Message = "Code verified successfully" });
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Phone == dto.PhoneNumber);
            if (user == null)
                return BadRequest(new { Message = "Phone number not found" });

            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { Message = "Passwords do not match" });

            if (string.IsNullOrEmpty(user.ResetCode) || user.ResetCodeExpiry < DateTime.Now)
                return BadRequest(new { Message = "Reset code is invalid or expired" });

            user.Password = _passwordHasher.HashPassword(dto.NewPassword);

            user.ResetCode = null;
            user.ResetCodeExpiry = null;

            _context.SaveChanges();

            return Ok(new { Message = "Password reset successfully" });
        }

        [HttpPost("employee/login")]
        public async Task<IActionResult> EmployeeLogin([FromBody] EmployeeLoginDto dto)
        {
            return await LoginEmployee(dto, EmployeeRole.Manager, EmployeeRole.OfficeEmployee);
        }

        [HttpPost("manager/login")]
        public async Task<IActionResult> ManagerLogin([FromBody] EmployeeLoginDto dto)
        {
            return await LoginEmployee(dto, EmployeeRole.Manager);
        }

        [HttpPost("staff/login")]
        public async Task<IActionResult> StaffLogin([FromBody] EmployeeLoginDto dto)
        {
            return await LoginEmployee(dto, EmployeeRole.OfficeEmployee);
        }

        [Authorize(Policy = AppPolicies.StaffOrManager)]
        [HttpGet("employee/me")]
        public async Task<IActionResult> EmployeeMe()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var accountType = User.FindFirst("AccountType")?.Value;

            if (employeeId == null || accountType != "Employee")
                return Unauthorized(new { Message = "Invalid token" });

            var employee = await _context.Employees.FindAsync(int.Parse(employeeId));
            if (employee == null)
                return Unauthorized(new { Message = "Employee not found" });

            return Ok(new
            {
                employee.Id,
                FullName = employee.FirstName + " " + employee.LastName,
                employee.Phone,
                employee.Role,
                employee.Status,
                employee.HireDate,
                employee.LicenseNumber
            });
        }

        [Authorize(Policy = AppPolicies.StaffOrManager)]
        [HttpPost("employee/logout")]
        public async Task<IActionResult> EmployeeLogout()
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var accountType = User.FindFirst("AccountType")?.Value;

            if (employeeId == null || accountType != "Employee")
                return Unauthorized(new { Message = "Invalid token" });

            var tokens = await _refreshTokenRepo.FindAsync(r =>
                r.EmployeeId == int.Parse(employeeId) &&
                !r.IsRevoked);

            if (tokens == null || !tokens.Any())
                return Ok(new { Message = "You are already logged out" });

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                _refreshTokenRepo.Update(token);
            }

            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new { Message = "Logged out successfully" });
        }

        [HttpPost("driver/login")]
        public async Task<IActionResult> DriverLogin([FromBody] EmployeeLoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var driver = await _context.Employees
                .FirstOrDefaultAsync(e => e.Phone == dto.Phone &&
                                          e.Role == EmployeeRole.Driver &&
                                          e.Status == EmployeeStatus.Active);

            if (driver == null)
                return Unauthorized(new { Message = "Login failed" });

            var passwordCheck = _passwordHasher.VerifyPassword(dto.Password, driver.Password);
            if (!passwordCheck)
                return Unauthorized(new { Message = "Login failed" });

            // هل لديه RefreshToken غير منتهي؟
            var existingRefresh = await _refreshTokenRepo.GetAsync(r =>
                r.EmployeeId == driver.Id &&
                r.ExpiresAt > DateTime.Now &&
                !r.IsRevoked);

            if (existingRefresh != null)
            {
                return Ok(new
                {
                    Message = "You are already logged in",
                    AccessToken = _tokenService.GenerateUserToken(driver),
                    RefreshToken = existingRefresh.Token,
                    Driver = new
                    {
                        driver.Id,
                        driver.FirstName,
                        driver.LastName,
                        driver.Phone,
                        driver.LicenseNumber,
                        driver.Status
                    }
                });
            }

            // إنشاء AccessToken + RefreshToken جديد
            var accessToken = _tokenService.GenerateUserToken(driver);
            var refreshToken = _tokenService.GenerateRefreshToken();

            var refresh = new RefreshToken
            {
                Token = refreshToken,
                EmployeeId = driver.Id,
                ExpiresAt = DateTime.Now.AddDays(7),
                IsRevoked = false
            };

            await _refreshTokenRepo.AddAsync(refresh);
            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new
            {
                Message = "Login successful",
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Driver = new
                {
                    driver.Id,
                    driver.FirstName,
                    driver.LastName,
                    driver.Phone,
                    driver.LicenseNumber,
                    driver.Status
                }
            });
        }

        [Authorize(Policy = AppPolicies.DriverOnly)]
        [HttpGet("driver/me")]
        public async Task<IActionResult> DriverMe()
        {
            var driverId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var accountType = User.FindFirst("AccountType")?.Value;

            if (driverId == null || accountType != "Employee")
                return Unauthorized(new { Message = "Invalid token" });

            var driver = await _context.Employees.FindAsync(int.Parse(driverId));
            if (driver == null || driver.Role != EmployeeRole.Driver)
                return Unauthorized(new { Message = "Driver not found" });

            return Ok(new
            {
                driver.Id,
                FullName = driver.FirstName + " " + driver.LastName,
                driver.Phone,
                driver.LicenseNumber,
                driver.Status,
                driver.HireDate
            });
        }

        [Authorize(Policy = AppPolicies.DriverOnly)]
        [HttpPost("driver/logout")]
        public async Task<IActionResult> DriverLogout()
        {
            var driverId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var accountType = User.FindFirst("AccountType")?.Value;

            if (driverId == null || accountType != "Employee")
                return Unauthorized(new { Message = "Invalid token" });

            var tokens = await _refreshTokenRepo.FindAsync(r =>
                r.EmployeeId == int.Parse(driverId) &&
                !r.IsRevoked);

            if (tokens == null || !tokens.Any())
                return Ok(new { Message = "You are already logged out" });

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                _refreshTokenRepo.Update(token);
            }

            await _refreshTokenRepo.SaveChangesAsync();

            return Ok(new { Message = "Logged out successfully" });
        }

        private async Task<IActionResult> LoginEmployee(
            EmployeeLoginDto dto,
            params EmployeeRole[] allowedRoles)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var employee = await _context.Employees.FirstOrDefaultAsync(e =>
                e.Phone == dto.Phone &&
                e.Status == EmployeeStatus.Active &&
                allowedRoles.Contains(e.Role));

            if (employee == null || !_passwordHasher.VerifyPassword(dto.Password, employee.Password))
                return Unauthorized(new { Message = "Login failed" });

            var existingRefresh = await _refreshTokenRepo.GetAsync(r =>
                r.EmployeeId == employee.Id &&
                r.ExpiresAt > DateTime.Now &&
                !r.IsRevoked);

            if (existingRefresh != null)
                return EmployeeLoginResponse(employee, existingRefresh.Token, "You are already logged in");

            var refreshToken = _tokenService.GenerateRefreshToken();
            await _refreshTokenRepo.AddAsync(new RefreshToken
            {
                Token = refreshToken,
                EmployeeId = employee.Id,
                ExpiresAt = DateTime.Now.AddDays(7),
                IsRevoked = false
            });
            await _refreshTokenRepo.SaveChangesAsync();

            return EmployeeLoginResponse(employee, refreshToken, "Login successful");
        }

        private IActionResult EmployeeLoginResponse(Employee employee, string refreshToken, string message)
        {
            return Ok(new
            {
                Message = message,
                AccessToken = _tokenService.GenerateUserToken(employee),
                RefreshToken = refreshToken,
                Employee = new
                {
                    employee.Id,
                    employee.FirstName,
                    employee.LastName,
                    employee.Phone,
                    employee.Role,
                    employee.Status
                }
            });
        }


    }
}


















