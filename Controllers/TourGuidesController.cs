using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R7alaAPI.Data;
using R7alaAPI.DTO;
using R7alaAPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace R7alaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TourGuidesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public TourGuidesController(
            ApplicationDBContext context,
            UserManager<User> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        /// <summary>
        /// Retrieves all tour guides.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllTourGuides()
        {
            var tourGuideRole = await _roleManager.FindByNameAsync("TourGuide");
            if (tourGuideRole == null)
                return NotFound(new { message = "TourGuide role not found" });

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID not found in token."));

            var tourGuides = await _context.TourGuides
                .Include(tg => tg.User)
                .Include(tg => tg.Reviews)
                .Join(
                    _context.UserRoles,
                    tg => tg.UserId,
                    ur => ur.UserId,
                    (tg, ur) => new { TourGuide = tg, UserRole = ur }
                )
                .Where(t => t.UserRole.RoleId == tourGuideRole.Id)
                .Select(t => new TourGuideDto
                {
                    Id = t.TourGuide.Id,
                    UserId = t.TourGuide.UserId,
                    FirstName = t.TourGuide.User.FirstName,
                    LastName = t.TourGuide.User.LastName,
                    Bio = t.TourGuide.Bio,
                    City = t.TourGuide.User.City,
                    YearsOfExperience = t.TourGuide.YearsOfExperience,
                    Languages = t.TourGuide.Languages,
                    HourlyRate = t.TourGuide.HourlyRate,
                    IsAvailable = t.TourGuide.IsAvailable,
                    ProfilePictureUrl = t.TourGuide.ProfilePictureUrl,
                    Stars = t.TourGuide.Reviews.Any() ? t.TourGuide.Reviews.Average(r => r.Rating) : 0.0,
                    IsFavorited = t.TourGuide.User.Favorites != null && t.TourGuide.User.Favorites.Any(f => f.TourGuideId == t.TourGuide.Id && f.UserId == currentUserId)
                })
                .ToListAsync();

            return Ok(tourGuides);
        }

        /// <summary>
        /// Retrieves tour guides with optional filters for city, languages, and max hourly rate.
        /// </summary>
        [HttpGet("filter")]
        public async Task<IActionResult> GetFilteredTourGuides(
            [FromQuery] string? city,
            [FromQuery] string? languages,
            [FromQuery] decimal? maxHourlyRate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1)
                return BadRequest(new { message = "Page number must be greater than 0" });

            if (pageSize < 1 || pageSize > 50)
                return BadRequest(new { message = "Page size must be between 1 and 50" });

            if (maxHourlyRate.HasValue && maxHourlyRate < 0)
                return BadRequest(new { message = "Maximum hourly rate cannot be negative" });

            var tourGuideRole = await _roleManager.FindByNameAsync("TourGuide");
            if (tourGuideRole == null)
                return NotFound(new { message = "TourGuide role not found" });

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("User ID not found in token."));

            var query = _context.TourGuides
                .Include(tg => tg.User)
                .Include(tg => tg.Reviews)
                .Join(
                    _context.UserRoles,
                    tg => tg.UserId,
                    ur => ur.UserId,
                    (tg, ur) => new { TourGuide = tg, UserRole = ur }
                )
                .Where(t => t.UserRole.RoleId == tourGuideRole.Id);

            // Apply filters
            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(t => t.TourGuide.User.City != null && t.TourGuide.User.City.ToLower() == city.ToLower());
            }

            if (maxHourlyRate.HasValue)
            {
                query = query.Where(t => t.TourGuide.HourlyRate <= maxHourlyRate.Value);
            }

            List<string> languageList = null;
            if (!string.IsNullOrWhiteSpace(languages))
            {
                languageList = languages.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim().ToLower())
                    .ToList();
            }

            // Get total count before pagination
            var totalItems = await query.CountAsync();

            // Fetch data with pagination
            var tourGuidesQuery = query
                .OrderBy(t => t.TourGuide.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TourGuideDto
                {
                    Id = t.TourGuide.Id,
                    UserId = t.TourGuide.UserId,
                    FirstName = t.TourGuide.User.FirstName,
                    LastName = t.TourGuide.User.LastName,
                    Bio = t.TourGuide.Bio,
                    City = t.TourGuide.User.City,
                    YearsOfExperience = t.TourGuide.YearsOfExperience,
                    Languages = t.TourGuide.Languages,
                    HourlyRate = t.TourGuide.HourlyRate,
                    IsAvailable = t.TourGuide.IsAvailable,
                    ProfilePictureUrl = t.TourGuide.ProfilePictureUrl,
                    Stars = t.TourGuide.Reviews.Any() ? t.TourGuide.Reviews.Average(r => r.Rating) : 0.0,
                    IsFavorited = t.TourGuide.User.Favorites != null && t.TourGuide.User.Favorites.Any(f => f.TourGuideId == t.TourGuide.Id && f.UserId == currentUserId)
                });

            var tourGuides = await tourGuidesQuery.ToListAsync();

            // Apply language filter in memory (if needed)
            if (languageList != null && languageList.Any())
            {
                tourGuides = tourGuides
                    .Where(tg => languageList.All(lang => tg.Languages.Any(l => l.ToLower() == lang)))
                    .ToList();
                totalItems = tourGuides.Count; // Adjust totalItems for in-memory filtering
            }

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var response = new
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Data = tourGuides
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTourGuideById(int id)
        {
            var tourGuide = await _context.TourGuides
                .Include(tg => tg.User)
                .ThenInclude(u => u.Favorites)
                .FirstOrDefaultAsync(tg => tg.Id == id);

            if (tourGuide == null)
                return NotFound(new { message = "Tour guide not found" });

            if (tourGuide.User == null)
                return StatusCode(500, new { message = "User data is missing for the tour guide" });

            if (!await _userManager.IsInRoleAsync(tourGuide.User, "TourGuide"))
                return BadRequest(new { message = "User is not a tour guide" });

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return Unauthorized(new { message = "User ID not found in token" });

            var tourGuideDto = new TourGuideDto
            {
                Id = tourGuide.Id,
                UserId = tourGuide.UserId,
                FirstName = tourGuide.User.FirstName,
                LastName = tourGuide.User.LastName,
                Bio = tourGuide.Bio,
                City = tourGuide.User.City,
                YearsOfExperience = tourGuide.YearsOfExperience,
                Languages = tourGuide.Languages,
                HourlyRate = tourGuide.HourlyRate,
                IsAvailable = tourGuide.IsAvailable,
                ProfilePictureUrl = tourGuide.ProfilePictureUrl,
                Stars = tourGuide.Reviews != null && tourGuide.Reviews.Any() ? tourGuide.Reviews.Average(r => r.Rating) : 0.0,
                IsFavorited = tourGuide.User.Favorites != null && tourGuide.User.Favorites.Any(f => f.TourGuideId == tourGuide.Id && f.UserId == int.Parse(currentUserId))
            };

            return Ok(tourGuideDto);
        }

        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetTourGuidesByName(string name)
        {
            var tourGuides = await _context.TourGuides
                .Include(tg => tg.User)
                .ThenInclude(u => u.Favorites)
                .Where(tg => tg.User.FirstName.ToLower().Contains(name.ToLower()) || tg.User.LastName.ToLower().Contains(name.ToLower()))
                .ToListAsync();

            if (!tourGuides.Any())
                return NotFound(new { message = "No tour guides found with the specified name" });

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId == null)
                return Unauthorized(new { message = "User ID not found in token" });

            var tourGuideDtos = new List<TourGuideDto>();
            foreach (var tourGuide in tourGuides)
            {
                if (tourGuide.User == null)
                    continue; // Skip if user data is missing

                if (!await _userManager.IsInRoleAsync(tourGuide.User, "TourGuide"))
                    continue; // Skip if user is not a tour guide

                var tourGuideDto = new TourGuideDto
                {
                    Id = tourGuide.Id,
                    UserId = tourGuide.UserId,
                    FirstName = tourGuide.User.FirstName,
                    LastName = tourGuide.User.LastName,
                    Bio = tourGuide.Bio,
                    City = tourGuide.User.City,
                    YearsOfExperience = tourGuide.YearsOfExperience,
                    Languages = tourGuide.Languages,
                    HourlyRate = tourGuide.HourlyRate,
                    IsAvailable = tourGuide.IsAvailable,
                    ProfilePictureUrl = tourGuide.ProfilePictureUrl,
                    Stars = tourGuide.Reviews != null && tourGuide.Reviews.Any() ? tourGuide.Reviews.Average(r => r.Rating) : 0.0,
                    IsFavorited = tourGuide.User.Favorites != null && tourGuide.User.Favorites.Any(f => f.TourGuideId == tourGuide.Id && f.UserId == int.Parse(currentUserId))
                };
                tourGuideDtos.Add(tourGuideDto);
            }

            if (!tourGuideDtos.Any())
                return NotFound(new { message = "No valid tour guides found with the specified name" });

            return Ok(tourGuideDtos);
        }

        [HttpPost("apply")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ApplyAsTourGuide([FromForm] TourGuideApplicationDto applicationDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Check for existing application
            if (await _context.TourGuideApplications.AnyAsync(tga => tga.UserId == userId && tga.Status != ApplicationStatus.Rejected))
                return BadRequest(new { message = "You already have a pending or accepted tour guide application" });

            // Check if already a tour guide
            if (await _context.TourGuideApplications.AnyAsync(tg => tg.UserId == userId))
                return BadRequest(new { message = "You are already a tour guide" });

            var application = new TourGuideApplication
            {
                UserId = userId,
                Bio = applicationDto.Bio,
                YearsOfExperience = applicationDto.YearsOfExperience,
                Languages = applicationDto.Languages,
                HourlyRate = applicationDto.HourlyRate,
                Status = ApplicationStatus.Pending,
                AdminComment = null
            };

            if (applicationDto.CV != null)
            {
                try
                {
                    var extension = Path.GetExtension(applicationDto.CV.FileName)?.ToLowerInvariant();
                    if (extension != ".pdf")
                        return BadRequest(new { message = "CV must be a PDF file" });

                    application.CVUrl = await SaveFile(applicationDto.CV, "cvs");
                }
                catch (Exception error)
                {
                    return BadRequest(new { message = $"Failed to save CV: {error.Message}" });
                }
            }
            else
            {
                return BadRequest(new { message = "CV is required" });
            }

            if (applicationDto.ProfilePicture != null)
            {
                try
                {
                    var extension = Path.GetExtension(applicationDto.ProfilePicture.FileName)?.ToLowerInvariant();
                    if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
                        return BadRequest(new { message = "Profile picture must be an image (jpg, jpeg, png, gif, webp)" });

                    application.ProfilePictureUrl = await SaveFile(applicationDto.ProfilePicture, "tourguides/profiles");
                }
                catch (Exception error)
                {
                    return BadRequest(new { message = $"Failed to save profile picture: {error.Message}" });
                }
            }

            try
            {
                _context.TourGuideApplications.Add(application);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Tour guide application submitted successfully" });
            }
            catch (Exception error)
            {
                return StatusCode(500, new { message = $"Failed to submit application: {error.Message}" });
            }
        }

        [HttpGet("my-application")]
        public async Task<IActionResult> GetMyApplication()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var application = await _context.TourGuideApplications
                .Include(tga => tga.User)
                .Where(tga => tga.UserId == userId)
                .Select(tga => new TourGuideApplicationResponseDto
                {
                    Id = tga.Id,
                    UserId = tga.UserId,
                    UserName = $"{tga.User.FirstName} {tga.User.LastName}",
                    Email = tga.User.Email,
                    Bio = tga.Bio,
                    YearsOfExperience = tga.YearsOfExperience,
                    Languages = tga.Languages,
                    HourlyRate = tga.HourlyRate,
                    CVUrl = tga.CVUrl,
                    ProfilePictureUrl = tga.ProfilePictureUrl,
                    Status = tga.Status,
                    AdminComment = tga.AdminComment,
                    SubmittedAt = tga.SubmittedAt,
                    ReviewedAt = tga.ReviewedAt
                })
                .FirstOrDefaultAsync();

            if (application == null)
                return NotFound(new { message = "No application found" });

            return Ok(application);
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetApplicationStatus()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var application = await _context.TourGuideApplications
                .Where(tga => tga.UserId == userId)
                .Select(tga => new { tga.Status })
                .FirstOrDefaultAsync();

            if (application == null)
                return NotFound(new { message = "No application found" });

            return Ok(new { Status = application.Status.ToString() });
        }

        [NonAction]
        private static async Task<string> SaveFile(IFormFile file, string subFolder)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            var allowedExtensions = subFolder.Contains("cvs")
                ? new[] { ".pdf" }
                : new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(extension))
                throw new ArgumentException($"Invalid file format. Supported formats: {string.Join(", ", allowedExtensions)}");

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
                throw new ArgumentException("File size exceeds 5MB limit");

            var uploadsFolder = Path.Combine("wwwroot", "Uploads", subFolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Uploads/{subFolder}/{uniqueFileName}";
        }
    }
}