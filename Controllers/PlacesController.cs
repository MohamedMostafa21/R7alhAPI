using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R7alaAPI.Data;
using R7alaAPI.DTO;
using R7alaAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace R7alaAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlacesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public PlacesController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost]
        //[Authorize(Roles = "Admin,TourGuide")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePlace([FromForm] PlaceCreateDto placeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Places.AnyAsync(p => p.Name == placeDto.Name && p.City == placeDto.City))
                return BadRequest(new { message = "Place with this name and city already exists" });

            var place = new Place
            {
                Name = placeDto.Name,
                Description = placeDto.Description,
                Type = placeDto.Type,
                City = placeDto.City,
                Country = placeDto.Country,
                Location = placeDto.Location,
                Latitude = placeDto.Latitude,
                Longitude = placeDto.Longitude,
                AveragePrice = placeDto.AveragePrice,
                ImageUrls = new List<string>()
            };

            try
            {
                place.ThumbnailUrl = await SaveFile(placeDto.Thumbnail, "places/thumbnails");
                if (placeDto.Images != null && placeDto.Images.Any())
                {
                    foreach (var image in placeDto.Images)
                    {
                        place.ImageUrls.Add(await SaveFile(image, "places/images"));
                    }
                }

                _context.Places.Add(place);
                await _context.SaveChangesAsync();

                var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
                return CreatedAtAction(nameof(GetPlaceById), new { id = place.Id }, new { message = "Place created successfully", place = MapToPlaceDto(place, userId, _context) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create place: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPlaces()
        {
            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var places = await _context.Places
                .Include(p => p.Reviews)
                .ToListAsync();

            var placeDtos = places.Select(p => MapToPlaceDto(p, userId, _context)).ToList();
            return Ok(placeDtos);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetPlaces(
            [FromQuery] string? city,
            [FromQuery] string? country,
            [FromQuery] string? type,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] double? minStars,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest(new { message = "Page and pageSize must be greater than 0." });

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var query = _context.Places
                .Include(p => p.Reviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(city))
                query = query.Where(p => p.City.Contains(city));

            if (!string.IsNullOrEmpty(country))
                query = query.Where(p => p.Country.Contains(country));

            if (!string.IsNullOrEmpty(type))
                query = query.Where(p => p.Type == type);

            if (minPrice.HasValue)
                query = query.Where(p => p.AveragePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.AveragePrice <= maxPrice.Value);

            if (minStars.HasValue)
                query = query.Where(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) >= minStars.Value : false);

            var totalPlaces = await query.CountAsync();
            var places = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var placeDtos = places.Select(p => MapToPlaceDto(p, userId, _context)).ToList();
            return Ok(new
            {
                TotalPlaces = totalPlaces,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalPlaces / (double)pageSize),
                Places = placeDtos
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlaceById(int id)
        {
            var place = await _context.Places
                .Include(p => p.Reviews)
                .Include(p => p.Activities)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (place == null)
                return NotFound(new { message = "Place not found" });

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var placeDto = MapToPlaceDto(place, userId, _context);
            placeDto.Activities = place.Activities.Select(a => new ActivityDto
            {
                Id = a.Id,
                PlaceId = a.PlaceId,
                Name = a.Name,
                Description = a.Description,
                Price = a.Price,
                ThumbnailUrl = a.ThumbnailUrl
            }).ToList();

            return Ok(placeDto);
        }

        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetPlacesByName(string name)
        {
            var places = await _context.Places
                .Include(p => p.Reviews)
                .Include(p => p.Activities)
                .Where(p => p.Name.ToLower().Contains(name.ToLower()))
                .ToListAsync();

            if (!places.Any())
                return NotFound(new { message = "No places found with the specified name" });

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var placeDtos = places.Select(place =>
            {
                var placeDto = MapToPlaceDto(place, userId, _context);
                placeDto.Activities = place.Activities.Select(a => new ActivityDto
                {
                    Id = a.Id,
                    PlaceId = a.PlaceId,
                    Name = a.Name,
                    Description = a.Description,
                    Price = a.Price,
                    ThumbnailUrl = a.ThumbnailUrl
                }).ToList();
                return placeDto;
            }).ToList();

            return Ok(placeDtos);
        }

        //[Authorize(Roles = "Admin,TourGuide")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePlace(int id, [FromForm] PlaceUpdateDto placeDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var place = await _context.Places
                .FirstOrDefaultAsync(p => p.Id == id);

            if (place == null)
                return NotFound(new { message = "Place not found" });

            try
            {
                if (!string.IsNullOrEmpty(placeDto.Name)) place.Name = placeDto.Name;
                if (!string.IsNullOrEmpty(placeDto.Description)) place.Description = placeDto.Description;
                if (!string.IsNullOrEmpty(placeDto.Type)) place.Type = placeDto.Type;
                if (!string.IsNullOrEmpty(placeDto.City)) place.City = placeDto.City;
                if (!string.IsNullOrEmpty(placeDto.Country)) place.Country = placeDto.Country;
                if (!string.IsNullOrEmpty(placeDto.Location)) place.Location = placeDto.Location;
                if (placeDto.Latitude.HasValue) place.Latitude = placeDto.Latitude.Value;
                if (placeDto.Longitude.HasValue) place.Longitude = placeDto.Longitude.Value;
                if (placeDto.AveragePrice.HasValue) place.AveragePrice = placeDto.AveragePrice.Value;

                if (placeDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(place.ThumbnailUrl))
                        DeleteFile(place.ThumbnailUrl);
                    place.ThumbnailUrl = await SaveFile(placeDto.Thumbnail, "places/thumbnails");
                }

                if (placeDto.Images != null && placeDto.Images.Any())
                {
                    foreach (var oldUrl in place.ImageUrls.ToList())
                        DeleteFile(oldUrl);
                    place.ImageUrls.Clear();
                    foreach (var image in placeDto.Images)
                        place.ImageUrls.Add(await SaveFile(image, "places/images"));
                }

                await _context.SaveChangesAsync();
                var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
                return Ok(new { message = "Place updated successfully", place = MapToPlaceDto(place, userId, _context) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update place: {ex.Message}" });
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlace(int id)
        {
            var place = await _context.Places
                .Include(p => p.Activities)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (place == null)
                return NotFound(new { message = "Place not found" });

            try
            {
                if (!string.IsNullOrEmpty(place.ThumbnailUrl))
                    DeleteFile(place.ThumbnailUrl);

                foreach (var url in place.ImageUrls)
                    DeleteFile(url);

                foreach (var activity in place.Activities.ToList())
                {
                    if (!string.IsNullOrEmpty(activity.ThumbnailUrl))
                        DeleteFile(activity.ThumbnailUrl);
                }

                _context.Places.Remove(place);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Place deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete place: {ex.Message}" });
            }
        }

        [NonAction]
        private async Task<string> SaveFile(IFormFile file, string subfolder)
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

        [NonAction]
        private void DeleteFile(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var filePath = Path.Combine("wwwroot", url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        [NonAction]
        private static PlaceDto MapToPlaceDto(Place place, int? userId, ApplicationDBContext context)
        {
            return new PlaceDto
            {
                Id = place.Id,
                Name = place.Name,
                Description = place.Description,
                Type = place.Type,
                City = place.City,
                Country = place.Country,
                Location = place.Location,
                Latitude = place.Latitude,
                Longitude = place.Longitude,
                AveragePrice = place.AveragePrice ?? 0.0m,
                Stars = place.Reviews != null && place.Reviews.Any()
                    ? Math.Round(place.Reviews.Average(r => r.Rating), 1)
                    : 0.0d,
                ThumbnailUrl = place.ThumbnailUrl,
                ImageUrls = place.ImageUrls,
                IsFavorited = userId.HasValue && context.Favorites.Any(l => l.UserId == userId && l.PlaceId == place.Id)
            };
        }
    }
}