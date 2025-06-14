using System;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

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