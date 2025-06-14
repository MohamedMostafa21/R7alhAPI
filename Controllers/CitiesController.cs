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
    public class CitiesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public CitiesController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateCity([FromForm] CityCreateDto cityDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Cities.AnyAsync(c => c.Name == cityDto.Name && c.Country == cityDto.Country))
            {
                return BadRequest(new { message = "City with this name and country already exists" });
            }

            var city = new City
            {
                Name = cityDto.Name,
                Country = cityDto.Country,
                Description = cityDto.Description,
                Latitude = cityDto.Latitude,
                Longitude = cityDto.Longitude
            };

            try
            {
                city.ThumbnailUrl = await SaveFile(cityDto.Thumbnail, "cities/thumbnails");
                if (cityDto.Images != null && cityDto.Images.Any())
                {
                    foreach (var image in cityDto.Images)
                    {
                        city.ImageUrls.Add(await SaveFile(image, "cities/images"));
                    }
                }

                _context.Cities.Add(city);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCityById), new { id = city.Id }, new
                {
                    message = "City created successfully",
                    city = new CityDto
                    {
                        Id = city.Id,
                        Name = city.Name,
                        Country = city.Country,
                        Description = city.Description,
                        Latitude = city.Latitude,
                        Longitude = city.Longitude,
                        ThumbnailUrl = city.ThumbnailUrl,
                        ImageUrls = city.ImageUrls
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create city: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCities()
        {
            var cities = await _context.Cities
                .Select(c => new CityDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Country = c.Country,
                    Description = c.Description,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    ThumbnailUrl = c.ThumbnailUrl,
                    ImageUrls = c.ImageUrls
                })
                .ToListAsync();

            return Ok(cities);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetCities(
            [FromQuery] string name,
            [FromQuery] string country,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest(new { message = "Page and pageSize must be greater than 0." });
            }

            var query = _context.Cities.AsQueryable();

            if (!string.IsNullOrEmpty(name))
                query = query.Where(c => c.Name.Contains(name));

            if (!string.IsNullOrEmpty(country))
                query = query.Where(c => c.Country.Contains(country));

            var totalCities = await query.CountAsync();
            var cities = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CityDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Country = c.Country,
                    Description = c.Description,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    ThumbnailUrl = c.ThumbnailUrl,
                    ImageUrls = c.ImageUrls
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCities = totalCities,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCities / (double)pageSize),
                Cities = cities
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCityById(int id)
        {
            var city = await _context.Cities
                .Select(c => new CityDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Country = c.Country,
                    Description = c.Description,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    ThumbnailUrl = c.ThumbnailUrl,
                    ImageUrls = c.ImageUrls
                })
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null)
            {
                return NotFound(new { message = "City not found" });
            }

            return Ok(city);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateCity(int id, [FromForm] CityUpdateDto cityDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null)
            {
                return NotFound(new { message = "City not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(cityDto.Name)) city.Name = cityDto.Name;
                if (!string.IsNullOrEmpty(cityDto.Country)) city.Country = cityDto.Country;
                if (!string.IsNullOrEmpty(cityDto.Description)) city.Description = cityDto.Description;
                if (cityDto.Latitude.HasValue) city.Latitude = cityDto.Latitude.Value;
                if (cityDto.Longitude.HasValue) city.Longitude = cityDto.Longitude.Value;

                if (cityDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(city.ThumbnailUrl))
                    {
                        DeleteFile(city.ThumbnailUrl);
                    }
                    city.ThumbnailUrl = await SaveFile(cityDto.Thumbnail, "cities/thumbnails");
                }

                if (cityDto.Images != null && cityDto.Images.Any())
                {
                    foreach (var oldUrl in city.ImageUrls.ToList())
                    {
                        DeleteFile(oldUrl);
                    }
                    city.ImageUrls.Clear();
                    foreach (var image in cityDto.Images)
                    {
                        city.ImageUrls.Add(await SaveFile(image, "cities/images"));
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    message = "City updated successfully",
                    city = new CityDto
                    {
                        Id = city.Id,
                        Name = city.Name,
                        Country = city.Country,
                        Description = city.Description,
                        Latitude = city.Latitude,
                        Longitude = city.Longitude,
                        ThumbnailUrl = city.ThumbnailUrl,
                        ImageUrls = city.ImageUrls
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update city: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.Id == id);

            if (city == null)
            {
                return NotFound(new { message = "City not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(city.ThumbnailUrl))
                {
                    DeleteFile(city.ThumbnailUrl);
                }

                foreach (var url in city.ImageUrls)
                {
                    DeleteFile(url);
                }

                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();

                return Ok(new { message = "City deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete city: {ex.Message}" });
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
    }
}