using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R7alaAPI.Data;
using R7alaAPI.DTO;
using R7alaAPI.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace R7alaAPI.Controllers
{
    [ApiController]
    [Route("api/places/{placeId}/activities")]
    public class ActivitiesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public ActivitiesController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateActivity(int placeId, [FromForm] ActivityCreateDto activityDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var place = await _context.Places.FindAsync(placeId);
            if (place == null)
                return NotFound(new { message = "Place not found" });

            var activity = new Activity
            {
                PlaceId = placeId,
                Name = activityDto.Name,
                Description = activityDto.Description,
                Price = activityDto.Price
            };

            try
            {
                if (activityDto.Thumbnail != null)
                    activity.ThumbnailUrl = await SaveFileAsync(activityDto.Thumbnail, "activities/thumbnails");

                _context.Activities.Add(activity);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetActivityById), new { placeId, id = activity.Id },
                    new { message = "Activity created successfully", activity = MapToActivityDto(activity) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create activity: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActivities(int placeId)
        {
            var place = await _context.Places.FindAsync(placeId);
            if (place == null)
                return NotFound(new { message = "Place not found" });

            var activities = await _context.Activities
                .Where(a => a.PlaceId == placeId)
                .ToListAsync();

            var activityDtos = activities.Select(MapToActivityDto).ToList();
            return Ok(activityDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetActivityById(int placeId, int id)
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.PlaceId == placeId && a.Id == id);

            if (activity == null)
                return NotFound(new { message = "Activity not found" });

            return Ok(MapToActivityDto(activity));
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateActivity(int placeId, int id, [FromForm] ActivityUpdateDto activityDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.PlaceId == placeId && a.Id == id);

            if (activity == null)
                return NotFound(new { message = "Activity not found" });

            try
            {
                activity.Name = activityDto.Name;
                activity.Description = activityDto.Description;
                activity.Price = activityDto.Price;

                if (activityDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(activity.ThumbnailUrl))
                        DeleteFile(activity.ThumbnailUrl);
                    activity.ThumbnailUrl = await SaveFileAsync(activityDto.Thumbnail, "activities/thumbnails");
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Activity updated successfully", activity = MapToActivityDto(activity) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update activity: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteActivity(int placeId, int id)
        {
            var activity = await _context.Activities
                .FirstOrDefaultAsync(a => a.PlaceId == placeId && a.Id == id);

            if (activity == null)
                return NotFound(new { message = "Activity not found" });

            try
            {
                if (!string.IsNullOrEmpty(activity.ThumbnailUrl))
                    DeleteFile(activity.ThumbnailUrl);

                _context.Activities.Remove(activity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Activity deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete activity: {ex.Message}" });
            }
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

        private static ActivityDto MapToActivityDto(Activity activity)
        {
            return new ActivityDto
            {
                Id = activity.Id,
                PlaceId = activity.PlaceId,
                Name = activity.Name,
                Description = activity.Description,
                Price = activity.Price,
                ThumbnailUrl = activity.ThumbnailUrl ?? string.Empty
            };
        }
    }
}