using System;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.DTO
{
    public class ReviewDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }
        public string? ProfilePictureUrl { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? PlaceId { get; set; }

        public string PlaceName { get; set; }

        public int? TourGuideId { get; set; }

        public string TourGuideName { get; set; }

        public int? HotelId { get; set; }

        public string HotelName { get; set; }

        public int? RestaurantId { get; set; }

        public string RestaurantName { get; set; }

        public int? PlanId { get; set; }

        public string PlanName { get; set; }
    }

    public class ReviewCreateDto
    {
        [Required]
        public string EntityType { get; set; } // e.g., "Place", "TourGuide", "Hotel", "Restaurant", "Plan"

        [Required]
        public int EntityId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }
    }

    public class ReviewUpdateDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }
    }
}