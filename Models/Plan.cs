using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Plan
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; } = 0m;

        [Required]
        public int Days { get; set; }

        [Url]
        public string? ThumbnailUrl { get; set; }

        [Required]
        public int? TourGuideId { get; set; }
        public TourGuide TourGuide { get; set; }

        public List<PlanPlace> PlanPlaces { get; set; } = new List<PlanPlace>();
    }
}