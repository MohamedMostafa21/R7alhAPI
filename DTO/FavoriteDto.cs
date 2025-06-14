using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.DTO
{
    public class FavoriteDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string UserName { get; set; }

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

    public class FavoriteCreateDto
    {
        [Required]
        public string EntityType { get; set; } // e.g., "Place", "TourGuide", "Hotel", "Restaurant", "Plan"

        [Required]
        public int EntityId { get; set; }
    }

    public class FavoriteDeleteDto
    {
        [Required]
        public string EntityType { get; set; }

        [Required]
        public int EntityId { get; set; }
    }
}