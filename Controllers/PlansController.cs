using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R7alaAPI.Data;
using R7alaAPI.DTO;
using R7alaAPI.Models;

namespace R7alaAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlansController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public PlansController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePlan([FromForm] PlanCreateDto planDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var tourGuide = await _context.TourGuides.FindAsync(planDto.TourGuideId);
            if (tourGuide == null)
                return NotFound(new { message = "Tour guide not found" });

            var plan = new Plan
            {
                Name = planDto.Name,
                Description = planDto.Description,
                Price = planDto.Price,
                Days = planDto.Days,
                TourGuideId = planDto.TourGuideId
            };

            try
            {
                if (planDto.Thumbnail != null)
                    plan.ThumbnailUrl = await SaveFileAsync(planDto.Thumbnail, "plans/thumbnails");

                _context.Plans.Add(plan);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPlanById), new { id = plan.Id },
                    new { message = "Plan created successfully", plan = MapToPlanDto(plan) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create plan: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _context.Plans
                .Include(p => p.TourGuide)
                .ThenInclude(tg => tg.User)
                .Include(p => p.PlanPlaces)
                .ThenInclude(pp => pp.Place)
                .ToListAsync();

            var planDtos = plans.Select(MapToPlanDto).ToList();
            return Ok(planDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlanById(int id)
        {
            var plan = await _context.Plans
                .Include(p => p.TourGuide)
                .ThenInclude(tg => tg.User)
                .Include(p => p.PlanPlaces)
                .ThenInclude(pp => pp.Place)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
                return NotFound(new { message = "Plan not found" });

            return Ok(MapToPlanDto(plan));
        }

        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetPlansByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Plan name cannot be empty" });

            var plans = await _context.Plans
                .Include(p => p.TourGuide)
                .ThenInclude(tg => tg.User)
                .Include(p => p.PlanPlaces)
                .ThenInclude(pp => pp.Place)
                .Where(p => p.Name.ToLower().Contains(name.ToLower()))
                .ToListAsync();

            if (!plans.Any())
                return NotFound(new { message = $"No plans found with name containing '{name}'" });

            var planDtos = plans.Select(MapToPlanDto).ToList();
            return Ok(planDtos);
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePlan(int id, [FromForm] PlanUpdateDto planDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var plan = await _context.Plans.FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plan not found" });

            var tourGuide = await _context.TourGuides.FindAsync(planDto.TourGuideId);
            if (tourGuide == null)
                return NotFound(new { message = "Tour guide not found" });

            try
            {
                plan.Name = planDto.Name;
                plan.Description = planDto.Description;
                plan.Price = planDto.Price;
                plan.Days = planDto.Days;
                plan.TourGuideId = planDto.TourGuideId;

                if (planDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(plan.ThumbnailUrl))
                        DeleteFile(plan.ThumbnailUrl);
                    plan.ThumbnailUrl = await SaveFileAsync(planDto.Thumbnail, "plans/thumbnails");
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Plan updated successfully", plan = MapToPlanDto(plan) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update plan: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.Plans.FindAsync(id);
            if (plan == null)
                return NotFound(new { message = "Plan not found" });

            try
            {
                if (!string.IsNullOrEmpty(plan.ThumbnailUrl))
                    DeleteFile(plan.ThumbnailUrl);

                _context.Plans.Remove(plan);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Plan deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete plan: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPost("{planId}/places")]
        public async Task<IActionResult> AddPlaceToPlan(int planId, [FromBody] PlanPlaceCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                );
                return BadRequest(new { message = "Validation failed", errors });
            }

            var plan = await _context.Plans.FindAsync(planId);
            if (plan == null)
                return NotFound(new { message = "Plan not found" });

            var place = await _context.Places.FindAsync(dto.PlaceId);
            if (place == null)
                return NotFound(new { message = "Place not found" });

            if (await _context.PlanPlaces.AnyAsync(pp => pp.PlanId == planId && pp.PlaceId == dto.PlaceId))
                return BadRequest(new { message = "Place already added to this plan" });

            var planPlace = new PlanPlace
            {
                PlanId = planId,
                PlaceId = dto.PlaceId,
                Order = dto.Order,
                Duration = dto.Duration,
                AdditionalDescription = dto.AdditionalDescription,
                SpecialPrice = dto.SpecialPrice
            };

            try
            {
                _context.PlanPlaces.Add(planPlace);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPlanPlaces), new { planId },
                    new { message = "Place added to plan successfully", planPlace = MapToPlanPlaceDto(planPlace) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to add place to plan: {ex.Message}" });
            }
        }

        [HttpGet("{planId}/places")]
        public async Task<IActionResult> GetPlanPlaces(int planId)
        {
            var plan = await _context.Plans.FindAsync(planId);
            if (plan == null)
                return NotFound(new { message = "Plan not found" });

            var planPlaces = await _context.PlanPlaces
                .Where(pp => pp.PlanId == planId)
                .Include(pp => pp.Place)
                .ToListAsync();

            var planPlaceDtos = planPlaces.Select(MapToPlanPlaceDto).ToList();
            return Ok(planPlaceDtos);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetPlans(
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] int? minDays,
            [FromQuery] int? maxDays,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest(new { message = "Page and pageSize must be greater than 0." });

            if (pageSize > 100)
                return BadRequest(new { message = "Page size cannot exceed 100." });

            if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
                return BadRequest(new { message = "Minimum price cannot be greater than maximum price." });

            if (minDays.HasValue && maxDays.HasValue && minDays > maxDays)
                return BadRequest(new { message = "Minimum days cannot be greater than maximum days." });

            if (minPrice.HasValue && minPrice < 0)
                return BadRequest(new { message = "Minimum price cannot be negative." });

            if (maxPrice.HasValue && maxPrice < 0)
                return BadRequest(new { message = "Maximum price cannot be negative." });

            if (minDays.HasValue && minDays < 1)
                return BadRequest(new { message = "Minimum days must be at least 1." });

            if (maxDays.HasValue && maxDays < 1)
                return BadRequest(new { message = "Maximum days must be at least 1." });

            var query = _context.Plans
                .Include(p => p.TourGuide)
                .ThenInclude(tg => tg.User)
                .Include(p => p.PlanPlaces)
                .ThenInclude(pp => pp.Place)
                .AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (minDays.HasValue)
                query = query.Where(p => p.Days >= minDays.Value);

            if (maxDays.HasValue)
                query = query.Where(p => p.Days <= maxDays.Value);

            var totalPlans = await query.CountAsync();
            var plans = await query
                .OrderBy(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var planDtos = plans.Select(MapToPlanDto).ToList();

            return Ok(new
            {
                TotalPlans = totalPlans,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalPlans / (double)pageSize),
                Plans = planDtos
            });
        }

        private async Task<string> SaveFileAsync(IFormFile file, string subfolder)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
                throw new ArgumentException("Invalid file format. Supported formats: jpg, jpeg, png, gif, webp.");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 5MB limit.");

            var uploadsFolder = Path.Combine("wwwroot", "Uploads", subfolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Uploads/{subfolder}/{uniqueFileName}";
        }

        private void DeleteFile(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var filePath = Path.Combine("wwwroot", url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        private PlanDto MapToPlanDto(Plan plan)
        {
            var planPlaces = _context.PlanPlaces
                .Where(pp => pp.PlanId == plan.Id)
                .Include(pp => pp.Place)
                .ToList();

            return new PlanDto
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                Price = plan.Price,
                Days = plan.Days,
                TourGuideId = plan.TourGuideId ?? 0,
                TourGuideName = plan.TourGuide?.User?.FirstName ?? string.Empty,
                ThumbnailUrl = plan.ThumbnailUrl ?? string.Empty,
                PlanPlaces = planPlaces.Select(MapToPlanPlaceDto).ToList()
            };
        }

        private static PlanPlaceDto MapToPlanPlaceDto(PlanPlace planPlace)
        {
            return new PlanPlaceDto
            {
                Id = planPlace.Id,
                PlanId = planPlace.PlanId,
                PlaceId = planPlace.PlaceId,
                PlaceName = planPlace.Place?.Name ?? string.Empty,
                ThumbnailUrl = planPlace.Place?.ThumbnailUrl ?? string.Empty,
                Order = planPlace.Order,
                Duration = planPlace.Duration,
                AdditionalDescription = planPlace.AdditionalDescription ?? string.Empty,
                SpecialPrice = planPlace.SpecialPrice
            };
        }
    }
}