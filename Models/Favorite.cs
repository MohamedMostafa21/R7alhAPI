using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; }

        public int? PlaceId { get; set; }

        public Place Place { get; set; }

        public int? TourGuideId { get; set; }

        public TourGuide TourGuide { get; set; }

        public int? HotelId { get; set; }

        public Hotel Hotel { get; set; }

        public int? RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; }

        public int? PlanId { get; set; }

        public Plan Plan { get; set; }
    }
}