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
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public ReviewsController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto reviewDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var entityType = reviewDto.EntityType.ToLower();

            Review review = null;
            bool entityExists = false;

            switch (entityType)
            {
                case "place":
                    entityExists = await _context.Places.AnyAsync(p => p.Id == reviewDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Reviews.AnyAsync(r => r.UserId == userId && r.PlaceId == reviewDto.EntityId))
                            return BadRequest(new { message = "Review already exists for this place" });
                        review = new Review
                        {
                            UserId = userId,
                            PlaceId = reviewDto.EntityId,
                            Rating = reviewDto.Rating,
                            Comment = reviewDto.Comment,
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                    break;
                case "tourguide":
                    entityExists = await _context.TourGuides.AnyAsync(tg => tg.Id == reviewDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Reviews.AnyAsync(r => r.UserId == userId && r.TourGuideId == reviewDto.EntityId))
                            return BadRequest(new { message = "Review already exists for this tour guide" });
                        review = new Review
                        {
                            UserId = userId,
                            TourGuideId = reviewDto.EntityId,
                            Rating = reviewDto.Rating,
                            Comment = reviewDto.Comment,
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                    break;
                case "hotel":
                    entityExists = await _context.Hotels.AnyAsync(h => h.Id == reviewDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Reviews.AnyAsync(r => r.UserId == userId && r.HotelId == reviewDto.EntityId))
                            return BadRequest(new { message = "Review already exists for this hotel" });
                        review = new Review
                        {
                            UserId = userId,
                            HotelId = reviewDto.EntityId,
                            Rating = reviewDto.Rating,
                            Comment = reviewDto.Comment,
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                    break;
                case "restaurant":
                    entityExists = await _context.Restaurants.AnyAsync(r => r.Id == reviewDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Reviews.AnyAsync(r => r.UserId == userId && r.RestaurantId == reviewDto.EntityId))
                            return BadRequest(new { message = "Review already exists for this restaurant" });
                        review = new Review
                        {
                            UserId = userId,
                            RestaurantId = reviewDto.EntityId,
                            Rating = reviewDto.Rating,
                            Comment = reviewDto.Comment,
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                    break;
                case "plan":
                    entityExists = await _context.Plans.AnyAsync(p => p.Id == reviewDto.EntityId);
                    if (entityExists)
                    {
                        if (await _context.Reviews.AnyAsync(r => r.UserId == userId && r.PlanId == reviewDto.EntityId))
                            return BadRequest(new { message = "Review already exists for this plan" });
                        review = new Review
                        {
                            UserId = userId,
                            PlanId = reviewDto.EntityId,
                            Rating = reviewDto.Rating,
                            Comment = reviewDto.Comment,
                            CreatedAt = DateTime.UtcNow
                        };
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
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Review created successfully", review = MapToReviewDto(review) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create review: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] ReviewUpdateDto reviewDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (review == null)
            {
                return NotFound(new { message = "Review not found or you are not authorized to update it" });
            }

            review.Rating = reviewDto.Rating;
            review.Comment = reviewDto.Comment;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Review updated successfully", review = MapToReviewDto(review) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update review: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (review == null)
            {
                return NotFound(new { message = "Review not found or you are not authorized to delete it" });
            }

            try
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Review deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete review: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllReviews(
            [FromQuery] float? minRating = null,
            [FromQuery] float? maxRating = null,
            [FromQuery] string sortOrder = "recent",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (minRating.HasValue && (minRating < 0 || minRating > 5))
                return BadRequest(new { message = "minRating must be between 0 and 5" });
            if (maxRating.HasValue && (maxRating < 0 || maxRating > 5))
                return BadRequest(new { message = "maxRating must be between 0 and 5" });
            if (minRating.HasValue && maxRating.HasValue && minRating > maxRating)
                return BadRequest(new { message = "minRating cannot be greater than maxRating" });
            if (page < 1)
                return BadRequest(new { message = "page must be at least 1" });
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { message = "pageSize must be between 1 and 100" });

            var query = _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Place)
                .Include(r => r.TourGuide).ThenInclude(tg => tg.User)
                .Include(r => r.Hotel)
                .Include(r => r.Restaurant)
                .Include(r => r.Plan)
                .AsQueryable();

            if (minRating.HasValue)
                query = query.Where(r => r.Rating >= minRating.Value);
            if (maxRating.HasValue)
                query = query.Where(r => r.Rating <= maxRating.Value);

            query = sortOrder.ToLower() == "oldest"
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User != null ? r.User.FirstName : null,
                    ProfilePictureUrl = r.User != null ? r.User.ProfilePictureUrl : null,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    PlaceId = r.PlaceId,
                    PlaceName = r.Place != null ? r.Place.Name : null,
                    TourGuideId = r.TourGuideId,
                    TourGuideName = r.TourGuide != null && r.TourGuide.User != null ? r.TourGuide.User.FirstName : null,
                    HotelId = r.HotelId,
                    HotelName = r.Hotel != null ? r.Hotel.Name : null,
                    RestaurantId = r.RestaurantId,
                    RestaurantName = r.Restaurant != null ? r.Restaurant.Name : null,
                    PlanId = r.PlanId,
                    PlanName = r.Plan != null ? r.Plan.Name : null
                })
                .ToListAsync();

            return Ok(new
            {
                data = reviews,
                pagination = new
                {
                    totalItems,
                    totalPages,
                    currentPage = page,
                    pageSize
                }
            });
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserReviews(
            [FromQuery] float? minRating = null,
            [FromQuery] float? maxRating = null,
            [FromQuery] string sortOrder = "recent",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (minRating.HasValue && (minRating < 0 || minRating > 5))
                return BadRequest(new { message = "minRating must be between 0 and 5" });
            if (maxRating.HasValue && (maxRating < 0 || maxRating > 5))
                return BadRequest(new { message = "maxRating must be between 0 and 5" });
            if (minRating.HasValue && maxRating.HasValue && minRating > maxRating)
                return BadRequest(new { message = "minRating cannot be greater than maxRating" });
            if (page < 1)
                return BadRequest(new { message = "page must be at least 1" });
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { message = "pageSize must be between 1 and 100" });

            var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

            var query = _context.Reviews
                .Where(r => r.UserId == userId)
                .Include(r => r.User)
                .Include(r => r.Place)
                .Include(r => r.TourGuide).ThenInclude(tg => tg.User)
                .Include(r => r.Hotel)
                .Include(r => r.Restaurant)
                .Include(r => r.Plan)
                .AsQueryable();

            if (minRating.HasValue)
                query = query.Where(r => r.Rating >= minRating.Value);
            if (maxRating.HasValue)
                query = query.Where(r => r.Rating <= maxRating.Value);

            query = sortOrder.ToLower() == "oldest"
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User != null ? r.User.FirstName : null,
                    ProfilePictureUrl = r.User != null ? r.User.ProfilePictureUrl : null,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    PlaceId = r.PlaceId,
                    PlaceName = r.Place != null ? r.Place.Name : null,
                    TourGuideId = r.TourGuideId,
                    TourGuideName = r.TourGuide != null && r.TourGuide.User != null ? r.TourGuide.User.FirstName : null,
                    HotelId = r.HotelId,
                    HotelName = r.Hotel != null ? r.Hotel.Name : null,
                    RestaurantId = r.RestaurantId,
                    RestaurantName = r.Restaurant != null ? r.Restaurant.Name : null,
                    PlanId = r.PlanId,
                    PlanName = r.Plan != null ? r.Plan.Name : null
                })
                .ToListAsync();

            return Ok(new
            {
                data = reviews,
                pagination = new
                {
                    totalItems,
                    totalPages,
                    currentPage = page,
                    pageSize
                }
            });
        }

        [HttpGet("{entityType}/{entityId}")]
        public async Task<IActionResult> GetReviewsForEntity(
            string entityType,
            int entityId,
            [FromQuery] float? minRating = null,
            [FromQuery] float? maxRating = null,
            [FromQuery] string sortOrder = "recent",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (minRating.HasValue && (minRating < 0 || minRating > 5))
                return BadRequest(new { message = "minRating must be between 0 and 5" });
            if (maxRating.HasValue && (maxRating < 0 || maxRating > 5))
                return BadRequest(new { message = "maxRating must be between 0 and 5" });
            if (minRating.HasValue && maxRating.HasValue && minRating > maxRating)
                return BadRequest(new { message = "minRating cannot be greater than maxRating" });
            if (page < 1)
                return BadRequest(new { message = "page must be at least 1" });
            if (pageSize < 1 || pageSize > 100)
                return BadRequest(new { message = "pageSize must be between 1 and 100" });

            entityType = entityType.ToLower();
            IQueryable<Review> query = null;

            switch (entityType)
            {
                case "place":
                    query = _context.Reviews.Where(r => r.PlaceId == entityId);
                    break;
                case "tourguide":
                    query = _context.Reviews.Where(r => r.TourGuideId == entityId);
                    break;
                case "hotel":
                    query = _context.Reviews.Where(r => r.HotelId == entityId);
                    break;
                case "restaurant":
                    query = _context.Reviews.Where(r => r.RestaurantId == entityId);
                    break;
                case "plan":
                    query = _context.Reviews.Where(r => r.PlanId == entityId);
                    break;
                default:
                    return BadRequest(new { message = "Invalid entity type" });
            }

            if (minRating.HasValue)
                query = query.Where(r => r.Rating >= minRating.Value);
            if (maxRating.HasValue)
                query = query.Where(r => r.Rating <= maxRating.Value);

            query = sortOrder.ToLower() == "oldest"
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var reviews = await query
                .Include(r => r.User)
                .Include(r => r.Place)
                .Include(r => r.TourGuide).ThenInclude(tg => tg.User)
                .Include(r => r.Hotel)
                .Include(r => r.Restaurant)
                .Include(r => r.Plan)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = r.User != null ? r.User.FirstName : null,
                    ProfilePictureUrl = r.User != null ? r.User.ProfilePictureUrl: null,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    PlaceId = r.PlaceId,
                    PlaceName = r.Place != null ? r.Place.Name : null,
                    TourGuideId = r.TourGuideId,
                    TourGuideName = r.TourGuide != null && r.TourGuide.User != null ? r.TourGuide.User.FirstName : null,
                    HotelId = r.HotelId,
                    HotelName = r.Hotel != null ? r.Hotel.Name : null,
                    RestaurantId = r.RestaurantId,
                    RestaurantName = r.Restaurant != null ? r.Restaurant.Name : null,
                    PlanId = r.PlanId,
                    PlanName = r.Plan != null ? r.Plan.Name : null
                })
                .ToListAsync();

            return Ok(new
            {
                data = reviews,
                pagination = new
                {
                    totalItems,
                    totalPages,
                    currentPage = page,
                    pageSize
                }
            });
        }

        [NonAction]
        private ReviewDto MapToReviewDto(Review review)
        {
            return new ReviewDto
            {
                Id = review.Id,
                UserId = review.UserId,
                UserName = review.User?.FirstName,
                ProfilePictureUrl = review.User?.ProfilePictureUrl,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                PlaceId = review.PlaceId, 
                PlaceName = review.Place?.Name,
                TourGuideId = review.TourGuideId,
                TourGuideName = review.TourGuide?.User?.FirstName,
                HotelId = review.HotelId,
                HotelName = review.Hotel?.Name,
                RestaurantId = review.RestaurantId,
                RestaurantName = review.Restaurant?.Name,
                PlanId = review.PlanId,
                PlanName = review.Plan?.Name
            };
        }
    }
}