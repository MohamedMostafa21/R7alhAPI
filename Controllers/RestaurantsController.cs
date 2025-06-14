using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
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
    public class RestaurantsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public RestaurantsController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateRestaurant([FromForm] RestaurantCreateDto restaurantDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _context.Restaurants.AnyAsync(r => r.Name == restaurantDto.Name && r.City == restaurantDto.City))
            {
                return BadRequest(new { message = "Restaurant with this name and city already exists" });
            }

            var restaurant = new Restaurant
            {
                Name = restaurantDto.Name,
                Description = restaurantDto.Description,
                City = restaurantDto.City,
                Country = restaurantDto.Country,
                Location = restaurantDto.Location,
                Latitude = restaurantDto.Latitude,
                Longitude = restaurantDto.Longitude,
                Cuisine = restaurantDto.Cuisine,
                AveragePrice = restaurantDto.AveragePrice
            };

            try
            {
                restaurant.ThumbnailUrl = await SaveFile(restaurantDto.Thumbnail, "restaurants/thumbnails");
                if (restaurantDto.Images != null && restaurantDto.Images.Any())
                {
                    foreach (var image in restaurantDto.Images)
                    {
                        restaurant.ImageUrls.Add(await SaveFile(image, "restaurants/images"));
                    }
                }

                _context.Restaurants.Add(restaurant);
                await _context.SaveChangesAsync();

                var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
                return CreatedAtAction(nameof(GetRestaurantById), new { id = restaurant.Id }, new { message = "Restaurant created successfully", restaurant = MapToRestaurantDto(restaurant, userId, _context) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create restaurant: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRestaurants()
        {
            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var restaurants = await _context.Restaurants
                .Include(r => r.Reviews)
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    City = r.City,
                    Country = r.Country,
                    Location = r.Location,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    Cuisine = r.Cuisine,
                    AveragePrice = r.AveragePrice,
                    Stars = r.Reviews != null && r.Reviews.Any()
                        ? Math.Round(r.Reviews.Average(r => r.Rating), 1)
                        : 0.0d,
                    ThumbnailUrl = r.ThumbnailUrl,
                    ImageUrls = r.ImageUrls,
                    IsFavorited = userId.HasValue && _context.Favorites.Any(f => f.UserId == userId && f.RestaurantId == r.Id)
                })
                .ToListAsync();

            return Ok(restaurants);
        }

        [HttpGet("filter")]
        public async Task<IActionResult> GetRestaurants(
            [FromQuery] string? city,
            [FromQuery] string? country,
            [FromQuery] string? cuisine,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] double? minStars,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest(new { message = "Page and pageSize must be greater than 0." });
            }

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var query = _context.Restaurants
                .Include(r => r.Reviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(city))
                query = query.Where(r => r.City.Contains(city));

            if (!string.IsNullOrEmpty(country))
                query = query.Where(r => r.Country.Contains(country));

            if (!string.IsNullOrEmpty(cuisine))
                query = query.Where(r => r.Cuisine.Contains(cuisine));

            if (minPrice.HasValue)
                query = query.Where(r => r.AveragePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(r => r.AveragePrice <= maxPrice.Value);

            if (minStars.HasValue)
                query = query.Where(r => r.Reviews.Any() ? r.Reviews.Average(r => r.Rating) >= minStars.Value : false);

            var totalRestaurants = await query.CountAsync();
            var restaurants = await query
                .OrderBy(r => r.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RestaurantDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description,
                    City = r.City,
                    Country = r.Country,
                    Location = r.Location,
                    Latitude = r.Latitude,
                    Longitude = r.Longitude,
                    Cuisine = r.Cuisine,
                    AveragePrice = r.AveragePrice,
                    Stars = r.Reviews != null && r.Reviews.Any()
                        ? Math.Round(r.Reviews.Average(r => r.Rating), 1)
                        : 0.0d,
                    ThumbnailUrl = r.ThumbnailUrl,
                    ImageUrls = r.ImageUrls,
                    IsFavorited = userId.HasValue && _context.Favorites.Any(f => f.UserId == userId && f.RestaurantId == r.Id)
                })
                .ToListAsync();

            return Ok(new
            {
                TotalRestaurants = totalRestaurants,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalRestaurants / (double)pageSize),
                Restaurants = restaurants
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRestaurantById(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Reviews)
                .Include(r => r.Meals)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound(new { message = "Restaurant not found" });
            }

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var restaurantDto = MapToRestaurantDto(restaurant, userId, _context);
            restaurantDto.Meals = restaurant.Meals.Select(m => new MealDto
            {
                Id = m.Id,
                RestaurantId = m.RestaurantId,
                Name = m.Name,
                Description = m.Description,
                Price = m.Price,
                ThumbnailUrl = m.ThumbnailUrl
            }).ToList();

            return Ok(restaurantDto);
        }

        [HttpGet("name/{name}")]
        public async Task<IActionResult> GetRestaurantsByName(string name)
        {
            var restaurants = await _context.Restaurants
                .Include(r => r.Reviews)
                .Include(r => r.Meals)
                .Where(r => r.Name.ToLower().Contains(name.ToLower()))
                .ToListAsync();

            if (!restaurants.Any())
                return NotFound(new { message = "No restaurants found with the specified name" });

            var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
            var restaurantDtos = restaurants.Select(restaurant =>
            {
                var restaurantDto = MapToRestaurantDto(restaurant, userId, _context);
                restaurantDto.Meals = restaurant.Meals.Select(m => new MealDto
                {
                    Id = m.Id,
                    RestaurantId = m.RestaurantId,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    ThumbnailUrl = m.ThumbnailUrl
                }).ToList();
                return restaurantDto;
            }).ToList();

            return Ok(restaurantDtos);
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateRestaurant(int id, [FromForm] RestaurantUpdateDto restaurantDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound(new { message = "Restaurant not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(restaurantDto.Name)) restaurant.Name = restaurantDto.Name;
                if (!string.IsNullOrEmpty(restaurantDto.Description)) restaurant.Description = restaurantDto.Description;
                if (!string.IsNullOrEmpty(restaurantDto.City)) restaurant.City = restaurantDto.City;
                if (!string.IsNullOrEmpty(restaurantDto.Country)) restaurant.City = restaurantDto.Country;
                if (!string.IsNullOrEmpty(restaurantDto.Location)) restaurant.Location = restaurantDto.Location;
                if (restaurantDto.Latitude.HasValue) restaurant.Latitude = restaurantDto.Latitude.Value;
                if (restaurantDto.Longitude.HasValue) restaurant.Longitude = restaurantDto.Longitude.Value;
                if (!string.IsNullOrEmpty(restaurantDto.Cuisine)) restaurant.Cuisine = restaurantDto.Cuisine;
                if (restaurantDto.AveragePrice.HasValue) restaurant.AveragePrice = restaurantDto.AveragePrice.Value;

                if (restaurantDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(restaurant.ThumbnailUrl))
                    {
                        DeleteFile(restaurant.ThumbnailUrl);
                    }
                    restaurant.ThumbnailUrl = await SaveFile(restaurantDto.Thumbnail, "restaurants/thumbnails");
                }

                if (restaurantDto.Images != null && restaurantDto.Images.Any())
                {
                    foreach (var oldUrl in restaurant.ImageUrls.ToList())
                    {
                        DeleteFile(oldUrl);
                    }
                    restaurant.ImageUrls.Clear();
                    foreach (var image in restaurantDto.Images)
                    {
                        restaurant.ImageUrls.Add(await SaveFile(image, "restaurants/images"));
                    }
                }

                await _context.SaveChangesAsync();
                var userId = User.Identity.IsAuthenticated ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value) : (int?)null;
                return Ok(new { message = "Restaurant updated successfully", restaurant = MapToRestaurantDto(restaurant, userId, _context) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update restaurant: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRestaurant(int id)
        {
            var restaurant = await _context.Restaurants
                .Include(r => r.Meals)
                .Include(r => r.Reviews)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound(new { message = "Restaurant not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(restaurant.ThumbnailUrl))
                {
                    DeleteFile(restaurant.ThumbnailUrl);
                }

                foreach (var url in restaurant.ImageUrls)
                {
                    DeleteFile(url);
                }

                foreach (var meal in restaurant.Meals.ToList())
                {
                    if (!string.IsNullOrEmpty(meal.ThumbnailUrl))
                    {
                        DeleteFile(meal.ThumbnailUrl);
                    }
                }

                _context.Restaurants.Remove(restaurant);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Restaurant deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete restaurant: {ex.Message}" });
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
        private static RestaurantDto MapToRestaurantDto(Restaurant restaurant, int? userId, ApplicationDBContext context)
        {
            return new RestaurantDto
            {
                Id = restaurant.Id,
                Name = restaurant.Name,
                Description = restaurant.Description,
                City = restaurant.City,
                Country = restaurant.Country,
                Location = restaurant.Location,
                Latitude = restaurant.Latitude,
                Longitude = restaurant.Longitude,
                Cuisine = restaurant.Cuisine,
                AveragePrice = restaurant.AveragePrice,
                Stars = restaurant.Reviews != null && restaurant.Reviews.Any()
                    ? Math.Round(restaurant.Reviews.Average(r => r.Rating), 1)
                    : 0.0d,
                ThumbnailUrl = restaurant.ThumbnailUrl,
                ImageUrls = restaurant.ImageUrls,
                IsFavorited = userId.HasValue && context.Favorites.Any(f => f.UserId == userId && f.RestaurantId == restaurant.Id)
            };
        }
    }
}