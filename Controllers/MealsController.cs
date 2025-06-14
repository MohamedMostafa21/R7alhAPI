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
    [Route("api/restaurants/{restaurantId}/meals")]
    public class MealsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public MealsController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateMeal(int restaurantId, [FromForm] MealCreateDto mealDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var restaurant = await _context.Restaurants.FindAsync(restaurantId);
            if (restaurant == null)
            {
                return NotFound(new { message = "Restaurant not found" });
            }

            var meal = new Meal
            {
                RestaurantId = restaurantId,
                Name = mealDto.Name,
                Description = mealDto.Description,
                Price = mealDto.Price
            };

            try
            {
                meal.ThumbnailUrl = await SaveFile(mealDto.Thumbnail, "meals/thumbnails");
                _context.Meals.Add(meal);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetMealById), new { restaurantId, id = meal.Id }, new { message = "Meal created successfully", meal = MapToMealDto(meal) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create meal: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMeals(int restaurantId)
        {
            var restaurant = await _context.Restaurants.FindAsync(restaurantId);
            if (restaurant == null)
            {
                return NotFound(new { message = "Restaurant not found" });
            }

            var meals = await _context.Meals
                .Where(m => m.RestaurantId == restaurantId)
                .Select(m => new MealDto
                {
                    Id = m.Id,
                    RestaurantId = m.RestaurantId,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    ThumbnailUrl = m.ThumbnailUrl
                })
                .ToListAsync();

            return Ok(meals);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMealById(int restaurantId, int id)
        {
            var meal = await _context.Meals
                .Where(m => m.RestaurantId == restaurantId && m.Id == id)
                .Select(m => new MealDto
                {
                    Id = m.Id,
                    RestaurantId = m.RestaurantId,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    ThumbnailUrl = m.ThumbnailUrl
                })
                .FirstOrDefaultAsync();

            if (meal == null)
            {
                return NotFound(new { message = "Meal not found" });
            }

            return Ok(meal);
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateMeal(int restaurantId, int id, [FromForm] MealUpdateDto mealDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var meal = await _context.Meals
                .FirstOrDefaultAsync(m => m.RestaurantId == restaurantId && m.Id == id);

            if (meal == null)
            {
                return NotFound(new { message = "Meal not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(mealDto.Name)) meal.Name = mealDto.Name;
                if (!string.IsNullOrEmpty(mealDto.Description)) meal.Description = mealDto.Description;
                if (mealDto.Price.HasValue) meal.Price = mealDto.Price.Value;

                if (mealDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(meal.ThumbnailUrl))
                    {
                        DeleteFile(meal.ThumbnailUrl);
                    }
                    meal.ThumbnailUrl = await SaveFile(mealDto.Thumbnail, "meals/thumbnails");
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Meal updated successfully", meal = MapToMealDto(meal) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update meal: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMeal(int restaurantId, int id)
        {
            var meal = await _context.Meals
                .FirstOrDefaultAsync(m => m.RestaurantId == restaurantId && m.Id == id);

            if (meal == null)
            {
                return NotFound(new { message = "Meal not found" });
            }

            try
            {
                if (!string.IsNullOrEmpty(meal.ThumbnailUrl))
                {
                    DeleteFile(meal.ThumbnailUrl);
                }

                _context.Meals.Remove(meal);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Meal deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete meal: {ex.Message}" });
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

            if (file.Length > 5 * 1024 * 1024)
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
        private MealDto MapToMealDto(Meal meal)
        {
            return new MealDto
            {
                Id = meal.Id,
                RestaurantId = meal.RestaurantId,
                Name = meal.Name,
                Description = meal.Description,
                Price = meal.Price,
                ThumbnailUrl = meal.ThumbnailUrl
            };
        }
    }
}