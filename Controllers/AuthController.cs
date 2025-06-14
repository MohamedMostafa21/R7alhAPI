using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using R7alaAPI.Data;
using R7alaAPI.DTO;
using R7alaAPI.Models;

namespace R7alaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDBContext _context;

        public AuthController(UserManager<User> userManager, IConfiguration configuration, ApplicationDBContext context)
        {
            _userManager = userManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("register")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Register([FromForm] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Validate DateOfBirth (optional, based on your preference)
            if (registerDto.DateOfBirth > DateTime.UtcNow.AddYears(-18))
            {
                return BadRequest(new { message = "User must be at least 18 years old" });
            }
            if (registerDto.DateOfBirth < DateTime.UtcNow.AddYears(-100))
            {
                return BadRequest(new { message = "Invalid Date of Birth" });
            }

            var user = new User
            {
                UserName = registerDto.Email,
                Email = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Gender = registerDto.Gender,
                DateOfBirth = registerDto.DateOfBirth,
                Country = registerDto.Country,
                City = registerDto.City,
                PhoneNumber = registerDto.PhoneNumber
            };

            if (registerDto.ProfilePicture != null)
            {
                try
                {
                    user.ProfilePictureUrl = await SaveFile(registerDto.ProfilePicture, "profiles");
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = $"Failed to save profile picture: {ex.Message}" });
                }
            }

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");
                return Ok(new
                {
                    message = "User registered successfully",
                    profilePictureUrl = user.ProfilePictureUrl
                });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
                return Unauthorized(new { message = "Invalid credentials" });

            var roles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var role in roles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = CreateToken(authClaims, loginDto.KeepMeSignedIn);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                user = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Gender,
                    user.Email,
                    user.ProfilePictureUrl,
                    user.Country,
                    user.City,
                    roles
                }
            });
        }
        [NonAction]
        private JwtSecurityToken CreateToken(List<Claim> authClaims, bool keepMeSignedIn)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
            var expiration = keepMeSignedIn
                ? DateTime.UtcNow.AddDays(_configuration.GetValue<int>("JwtSettings:LongTokenExpirationDays"))
                : DateTime.UtcNow.AddHours(_configuration.GetValue<int>("JwtSettings:TokenExpirationHours"));

            return new JwtSecurityToken(
                issuer: _configuration["JwtSettings:ValidIssuer"],
                audience: _configuration["JwtSettings:ValidAudience"],
                expires: expiration,
                claims: authClaims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );
        }
        [NonAction]
        public async Task<string> SaveFile(IFormFile file, string subfolder)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var allowedExtensions = subfolder.Contains("cvs")
                ? new[] { ".pdf" }
                : new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"Invalid file format. Supported formats: {string.Join(", ", allowedExtensions)}.");
            }

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                throw new ArgumentException("File size exceeds 5MB limit.");
            }

            var uploadsFolder = Path.Combine("wwwroot", "Uploads", subfolder);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Uploads/{subfolder}/{uniqueFileName}";
        }
    }
}