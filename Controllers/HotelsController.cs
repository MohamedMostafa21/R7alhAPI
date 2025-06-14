using Microsoft.AspNetCore.Authorization;
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
    public class HotelsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public HotelsController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateHotel([FromForm] HotelCreateDto hotelDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Hotels.AnyAsync(h => h.Name == hotelDto.Name && h.City == hotelDto.City))
                return BadRequest(new { message = "Hotel not found with this name and city already exists" });

            var hotel = new Hotel
            {
                Name = hotelDto.Name,
                Description = hotelDto.Description,
                City = hotelDto.City,
                Country = hotelDto.Country,
                Location = hotelDto.Location,
                Latitude = hotelDto.Latitude,
                Longitude = hotelDto.Longitude,
                StartingPrice = hotelDto.StartingPrice,
                Rate = hotelDto.Rate,
                ImageUrls = new List<string>()
            };

            try
            {
                hotel.ThumbnailUrl = await SaveFile(hotelDto.Thumbnail, "hotels/thumbnails");
                if (hotelDto.Images != null && hotelDto.Images.Any())
                {
                    foreach (var image in hotelDto.Images)
                    {
                        hotel.ImageUrls.Add(await SaveFile(image, "hotels/images"));
                    }
                }

                _context.Hotels.Add(hotel);
                await _context.SaveChangesAsync();

                var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
                return CreatedAtAction(nameof(GetHotelById), new { id = hotel.Id }, new { message = "Hotel created successfully", hotel = MapToHotelDto(hotel, userId, _context) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create hotel: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllHotels()
        {
            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var hotels = await _context.Hotels
                .Include(h => h.Reviews)
                .ToListAsync();

            var hotelDtos = hotels.Select(h => MapToHotelDto(h, userId, _context)).ToList();
            return Ok(hotelDtos);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetHotels(
            [FromQuery] string? city,
            [FromQuery] string? country,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] double? minStars,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
                return BadRequest(new { message = "Page and pageSize must be greater than 0." });

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var query = _context.Hotels
                .Include(h => h.Reviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(city))
                query = query.Where(h => h.City.Contains(city));

            if (!string.IsNullOrEmpty(country))
                query = query.Where(h => h.Country.Contains(country));

            if (minPrice.HasValue)
                query = query.Where(h => h.StartingPrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(h => h.StartingPrice <= maxPrice.Value);

            if (minStars.HasValue)
                query = query.Where(h => h.Reviews.Any() ? h.Reviews.Average(r => r.Rating) >= minStars.Value : false);

            var totalHotels = await query.CountAsync();
            var hotels = await query
                .OrderBy(h => h.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var hotelDtos = hotels.Select(h => MapToHotelDto(h, userId, _context)).ToList();
            return Ok(new
            {
                TotalHotels = totalHotels,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalHotels / (double)pageSize),
                Hotels = hotelDtos
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetHotelById(int id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Reviews)
                .Include(h => h.Rooms)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var hotelDto = MapToHotelDto(hotel, userId, _context);
            hotelDto.Rooms = hotel.Rooms.Select(r => new RoomDto
            {
                Id = r.Id,
                HotelId = r.HotelId,
                Type = r.Type,
                Capacity = r.Capacity,
                PricePerNight = r.PricePerNight,
                Description = r.Description,
                IsAvailable = r.IsAvailable,
                ThumbnailUrl = r.ThumbnailUrl,
                ImageUrls = r.ImageUrls
            }).ToList();

            return Ok(hotelDto);
        }

        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetHotelsByName(string name)
        {
            var hotels = await _context.Hotels
                .Include(h => h.Reviews)
                .Include(h => h.Rooms)
                .Where(h => h.Name.ToLower().Contains(name.ToLower()))
                .ToListAsync();

            if (!hotels.Any())
                return NotFound(new { message = "No hotels found with the specified name" });

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var hotelDtos = hotels.Select(hotel =>
            {
                var hotelDto = MapToHotelDto(hotel, userId, _context);
                hotelDto.Rooms = hotel.Rooms.Select(r => new RoomDto
                {
                    Id = r.Id,
                    HotelId = r.HotelId,
                    Type = r.Type,
                    Capacity = r.Capacity,
                    PricePerNight = r.PricePerNight,
                    Description = r.Description,
                    IsAvailable = r.IsAvailable,
                    ThumbnailUrl = r.ThumbnailUrl,
                    ImageUrls = r.ImageUrls
                }).ToList();
                return hotelDto;
            }).ToList();

            return Ok(hotelDtos);
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateHotel(int id, [FromForm] HotelUpdateDto hotelDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            try
            {
                if (!string.IsNullOrEmpty(hotelDto.Name)) hotel.Name = hotelDto.Name;
                if (!string.IsNullOrEmpty(hotelDto.Description)) hotel.Description = hotelDto.Description;
                if (!string.IsNullOrEmpty(hotelDto.City)) hotel.City = hotelDto.City;
                if (!string.IsNullOrEmpty(hotelDto.Country)) hotel.Country = hotelDto.Country;
                if (!string.IsNullOrEmpty(hotelDto.Location)) hotel.Location = hotelDto.Location;
                if (hotelDto.Latitude.HasValue) hotel.Latitude = hotelDto.Latitude.Value;
                if (hotelDto.Longitude.HasValue) hotel.Longitude = hotelDto.Longitude.Value;
                if (hotelDto.StartingPrice.HasValue) hotel.StartingPrice = hotelDto.StartingPrice.Value;
                if (hotelDto.Rate.HasValue) hotel.Rate = hotelDto.Rate.Value;

                if (hotelDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(hotel.ThumbnailUrl))
                        DeleteFile(hotel.ThumbnailUrl);
                    hotel.ThumbnailUrl = await SaveFile(hotelDto.Thumbnail, "hotels/thumbnails");
                }

                if (hotelDto.Images != null && hotelDto.Images.Any())
                {
                    foreach (var oldUrl in hotel.ImageUrls.ToList())
                        DeleteFile(oldUrl);
                    hotel.ImageUrls.Clear();
                    foreach (var image in hotelDto.Images)
                        hotel.ImageUrls.Add(await SaveFile(image, "hotels/images"));
                }

                await _context.SaveChangesAsync();
                var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
                return Ok(new { message = "Hotel updated successfully", hotel = MapToHotelDto(hotel, userId, _context) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update hotel: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHotel(int id)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Rooms)
                .Include(h => h.Reviews)
                .FirstOrDefaultAsync(h => h.Id == id);

            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            try
            {
                if (!string.IsNullOrEmpty(hotel.ThumbnailUrl))
                    DeleteFile(hotel.ThumbnailUrl);

                foreach (var url in hotel.ImageUrls)
                    DeleteFile(url);

                foreach (var room in hotel.Rooms.ToList())
                {
                    if (!string.IsNullOrEmpty(room.ThumbnailUrl))
                        DeleteFile(room.ThumbnailUrl);
                    foreach (var roomImage in room.ImageUrls)
                        DeleteFile(roomImage);
                }

                _context.Hotels.Remove(hotel);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Hotel deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete hotel: {ex.Message}" });
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
        private static HotelDto MapToHotelDto(Hotel hotel, int? userId, ApplicationDBContext context)
        {
            return new HotelDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                Description = hotel.Description,
                City = hotel.City,
                Country = hotel.Country,
                Location = hotel.Location,
                Latitude = hotel.Latitude,
                Longitude = hotel.Longitude,
                StartingPrice = hotel.StartingPrice,
                Rate = hotel.Rate,
                Stars = hotel.Reviews != null && hotel.Reviews.Any()
                    ? Math.Round(hotel.Reviews.Average(r => r.Rating), 1)
                    : 0.0d,
                ThumbnailUrl = hotel.ThumbnailUrl,
                ImageUrls = hotel.ImageUrls,
                IsFavorited = userId.HasValue && context.Favorites.Any(l => l.UserId == userId && l.HotelId == hotel.Id)
            };
        }
    }
}