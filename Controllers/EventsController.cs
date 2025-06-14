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
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public EventsController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateEvent([FromForm] EventCreateDto eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (eventDto.EndDate <= eventDto.StartDate)
            {
                return BadRequest(new { message = "End date must be after start date" });
            }

            var @event = new Event
            {
                Name = eventDto.Name,
                Description = eventDto.Description,
                StartDate = eventDto.StartDate,
                EndDate = eventDto.EndDate,
                City = eventDto.City,
                Location = eventDto.Location,
                Latitude = eventDto.Latitude,
                Longitude = eventDto.Longitude,
                Price = eventDto.Price
            };

            try
            {
                @event.ThumbnailUrl = await SaveFile(eventDto.Thumbnail, "events/thumbnails");

                _context.Events.Add(@event);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEventById), new { id = @event.Id }, new { message = "Event created successfully", @event = MapToEventDto(@event) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create event: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var events = await _context.Events
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    City = e.City,
                    Location = e.Location,
                    Latitude = e.Latitude,
                    Longitude = e.Longitude,
                    Price = e.Price,
                    ThumbnailUrl = e.ThumbnailUrl
                })
                .ToListAsync();

            return Ok(new { data = events });
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetEvents(
            [FromQuery] string name = null,
            [FromQuery] string city = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0)
                return BadRequest(new { message = "Page must be greater than 0" });
            if (pageSize <= 0 || pageSize > 100)
                return BadRequest(new { message = "PageSize must be between 1 and 100" });

            var query = _context.Events.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(e => e.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(e => e.City.Contains(city, StringComparison.OrdinalIgnoreCase));

            if (startDate.HasValue)
                query = query.Where(e => e.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(e => e.EndDate <= endDate.Value);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var events = await query
                .OrderBy(e => e.StartDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EventDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Description = e.Description,
                    StartDate = e.StartDate,
                    EndDate = e.EndDate,
                    City = e.City,
                    Location = e.Location,
                    Latitude = e.Latitude,
                    Longitude = e.Longitude,
                    Price = e.Price,
                    ThumbnailUrl = e.ThumbnailUrl
                })
                .ToListAsync();

            return Ok(new
            {
                data = events,
                pagination = new
                {
                    totalItems,
                    totalPages,
                    currentPage = page,
                    pageSize
                }
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(int id)
        {
            var @event = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            return Ok(new EventDto
            {
                Id = @event.Id,
                Name = @event.Name,
                Description = @event.Description,
                StartDate = @event.StartDate,
                EndDate = @event.EndDate,
                City = @event.City,
                Location = @event.Location,
                Latitude = @event.Latitude,
                Longitude = @event.Longitude,
                Price = @event.Price,
                ThumbnailUrl = @event.ThumbnailUrl
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateEvent(int id, [FromForm] EventUpdateDto eventDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var @event = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(eventDto.Name)) @event.Name = eventDto.Name;
                if (!string.IsNullOrEmpty(eventDto.Description)) @event.Description = eventDto.Description;
                if (eventDto.StartDate.HasValue) @event.StartDate = eventDto.StartDate.Value;
                if (eventDto.EndDate.HasValue) @event.EndDate = eventDto.EndDate.Value;
                if (!string.IsNullOrEmpty(eventDto.City)) @event.City = eventDto.City;
                if (!string.IsNullOrEmpty(eventDto.Location)) @event.Location = eventDto.Location;
                if (eventDto.Latitude.HasValue) @event.Latitude = eventDto.Latitude.Value;
                if (eventDto.Longitude.HasValue) @event.Longitude = eventDto.Longitude.Value;
                if (eventDto.Price.HasValue) @event.Price = eventDto.Price.Value;

                if (eventDto.StartDate.HasValue && eventDto.EndDate.HasValue && eventDto.EndDate <= eventDto.StartDate)
                {
                    return BadRequest(new { message = "End date must be after start date" });
                }

                if (eventDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(@event.ThumbnailUrl))
                    {
                        DeleteFile(@event.ThumbnailUrl);
                    }
                    @event.ThumbnailUrl = await SaveFile(eventDto.Thumbnail, "events/thumbnails");
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Event updated successfully", @event = MapToEventDto(@event) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update event: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var @event = await _context.Events
                .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                return NotFound(new { message = "Event not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(@event.ThumbnailUrl))
                {
                    DeleteFile(@event.ThumbnailUrl);
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Event deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete event: {ex.Message}" });
            }
        }

        [NonAction]
        private async Task<string> SaveFile(IFormFile file, string subfolder)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
            {
                throw new ArgumentException("Invalid file format. Supported formats: jpg, jpeg, png, gif, webp.");
            }

            if (file.Length > 5 * 1024 * 1024) // 5MB limit
            {
                throw new ArgumentException("File size exceeds 5MB limit.");
            }

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

        [NonAction]
        private void DeleteFile(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var filePath = Path.Combine("wwwroot", url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        [NonAction]
        private static EventDto MapToEventDto(Event @event)
        {
            return new EventDto
            {
                Id = @event.Id,
                Name = @event.Name,
                Description = @event.Description,
                StartDate = @event.StartDate,
                EndDate = @event.EndDate,
                City = @event.City,
                Location = @event.Location,
                Latitude = @event.Latitude,
                Longitude = @event.Longitude,
                Price = @event.Price,
                ThumbnailUrl = @event.ThumbnailUrl
            };
        }
    }
}