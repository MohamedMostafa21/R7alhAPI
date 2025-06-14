using System;
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
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public FavoritesController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFavorite([FromBody] FavoriteCreateDto favoriteDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var entityType = favoriteDto.EntityType.ToLower();

            Favorite favorite = null;
            bool entityExists = false;

            switch (entityType)
            {
                case "place":
                    entityExists = await _context.Places.AnyAsync(p => p.Id == favoriteDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Favorites.AnyAsync(f => f.UserId == userId && f.PlaceId == favoriteDto.EntityId))
                            return BadRequest(new { message = "Favorite already exists for this place" });
                        favorite = new Favorite { UserId = userId, PlaceId = favoriteDto.EntityId };
                    }
                    break;
                case "tourguide":
                    entityExists = await _context.TourGuides.AnyAsync(tg => tg.Id == favoriteDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Favorites.AnyAsync(f => f.UserId == userId && f.TourGuideId == favoriteDto.EntityId))
                            return BadRequest(new { message = "Favorite already exists for this tour guide" });
                        favorite = new Favorite { UserId = userId, TourGuideId = favoriteDto.EntityId };
                    }
                    break;
                case "hotel":
                    entityExists = await _context.Hotels.AnyAsync(h => h.Id == favoriteDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Favorites.AnyAsync(f => f.UserId == userId && f.HotelId == favoriteDto.EntityId))
                            return BadRequest(new { message = "Favorite already exists for this hotel" });
                        favorite = new Favorite { UserId = userId, HotelId = favoriteDto.EntityId };
                    }
                    break;
                case "restaurant":
                    entityExists = await _context.Restaurants.AnyAsync(r => r.Id == favoriteDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Favorites.AnyAsync(f => f.UserId == userId && f.RestaurantId == favoriteDto.EntityId))
                            return BadRequest(new { message = "Favorite already exists for this restaurant" });
                        favorite = new Favorite { UserId = userId, RestaurantId = favoriteDto.EntityId };
                    }
                    break;
                case "plan":
                    entityExists = await _context.Plans.AnyAsync(p => p.Id == favoriteDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Favorites.AnyAsync(f => f.UserId == userId && f.PlanId == favoriteDto.EntityId))
                            return BadRequest(new { message = "Favorite already exists for this plan" });
                        favorite = new Favorite { UserId = userId, PlanId = favoriteDto.EntityId };
                    }
                    break;
                default:
                    return BadRequest(new { message = "Invalid entity type" });
            }

            if (!entityExists)
            {
                return NotFound(new { message = $"{entityType} not found" });
            }

            try
            {
                _context.Favorites.Add(favorite);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Favorite created successfully", favorite = MapToFavoriteDto(favorite) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create favorite: {ex.Message}" });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFavorite([FromBody] FavoriteDeleteDto favoriteDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var entityType = favoriteDto.EntityType.ToLower();

            Favorite favorite = null;

            switch (entityType)
            {
                case "place":
                    favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.PlaceId == favoriteDto.EntityId);
                    break;
                case "tourguide":
                    favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.TourGuideId == favoriteDto.EntityId);
                    break;
                case "hotel":
                    favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.HotelId == favoriteDto.EntityId);
                    break;
                case "restaurant":
                    favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.RestaurantId == favoriteDto.EntityId);
                    break;
                case "plan":
                    favorite = await _context.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.PlanId == favoriteDto.EntityId);
                    break;
                default:
                    return BadRequest(new { message = "Invalid entity type" });
            }

            if (favorite == null)
            {
                return NotFound(new { message = "Favorite not found" });
            }

            try
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Favorite deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete favorite: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAllFavorites()
        {
            var favorites = await _context.Favorites
                .Include(f => f.User)
                .Include(f => f.Place)
                .Include(f => f.TourGuide)
                .Include(f => f.Hotel)
                .Include(f => f.Restaurant)
                .Include(f => f.Plan)
                .ToListAsync();

            var favoriteDtos = favorites.Select(MapToFavoriteDto).ToList();
            return Ok(favoriteDtos);
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserFavorites()
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.User)
                .Include(f => f.Place)
                .Include(f => f.TourGuide)
                .Include(f => f.Hotel)
                .Include(f => f.Restaurant)
                .Include(f => f.Plan)
                .ToListAsync();

            var favoriteDtos = favorites.Select(MapToFavoriteDto).ToList();
            return Ok(favoriteDtos);
        }

        [HttpGet("{entityType}/{entityId}")]
        public async Task<IActionResult> GetFavoritesForEntity(string entityType, int entityId)
        {
            entityType = entityType.ToLower();
            IQueryable<Favorite> query = null;

            switch (entityType)
            {
                case "place":
                    query = _context.Favorites.Where(f => f.PlaceId == entityId);
                    break;
                case "tourguide":
                    query = _context.Favorites.Where(f => f.TourGuideId == entityId);
                    break;
                case "hotel":
                    query = _context.Favorites.Where(f => f.HotelId == entityId);
                    break;
                case "restaurant":
                    query = _context.Favorites.Where(f => f.RestaurantId == entityId);
                    break;
                case "plan":
                    query = _context.Favorites.Where(f => f.PlanId == entityId);
                    break;
                default:
                    return BadRequest(new { message = "Invalid entity type" });
            }

            var favorites = await query
                .Include(f => f.User)
                .Include(f => f.Place)
                .Include(f => f.TourGuide)
                .Include(f => f.Hotel)
                .Include(f => f.Restaurant)
                .Include(f => f.Plan)
                .ToListAsync();

            var favoriteDtos = favorites.Select(MapToFavoriteDto).ToList();
            return Ok(favoriteDtos);
        }

        [NonAction]
        private static FavoriteDto MapToFavoriteDto(Favorite favorite)
        {
            return new FavoriteDto
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                UserName = favorite.User?.UserName,
                PlaceId = favorite.PlaceId,
                PlaceName = favorite.Place?.Name,
                TourGuideId = favorite.TourGuideId,
                TourGuideName = favorite.TourGuide?.User?.UserName,
                HotelId = favorite.HotelId,
                HotelName = favorite.Hotel?.Name,
                RestaurantId = favorite.RestaurantId,
                RestaurantName = favorite.Restaurant?.Name,
                PlanId = favorite.PlanId,
                PlanName = favorite.Plan?.Name
            };
        }
    }
}