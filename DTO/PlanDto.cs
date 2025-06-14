using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R7alaAPI.DTO
{
    public class PlanCreateDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public decimal Price { get; set; } = 0m;

        [Required, Range(1, int.MaxValue, ErrorMessage = "Days must be at least 1")]
        public int Days { get; set; }

        [Required]
        public int TourGuideId { get; set; }

        public IFormFile? Thumbnail { get; set; }
    }

    public class PlanUpdateDto
    {
        [Required, MaxLength(200)]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        public decimal Price { get; set; } = 0m;

        [Required, Range(1, int.MaxValue, ErrorMessage = "Days must be at least 1")]
        public int Days { get; set; }

        [Required]
        public int TourGuideId { get; set; }

        public IFormFile? Thumbnail { get; set; }
    }

    public class PlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Days { get; set; }
        public int TourGuideId { get; set; }
        public string TourGuideName { get; set; }
        public string ThumbnailUrl { get; set; }
        public List<PlanPlaceDto> PlanPlaces { get; set; } = new List<PlanPlaceDto>();
    }

    public class PlanPlaceCreateDto
    {
        [Required]
        public int PlaceId { get; set; }

        public int? Order { get; set; }

        public TimeSpan? Duration { get; set; }

        public string? AdditionalDescription { get; set; }

        public decimal SpecialPrice { get; set; } = 0m;
    }

    public class PlanPlaceDto
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public int PlaceId { get; set; }
        public string PlaceName { get; set; }
        public string ThumbnailUrl { get; set; }
        public int? Order { get; set; }
        public TimeSpan? Duration { get; set; }
        public string AdditionalDescription { get; set; }
        public decimal SpecialPrice { get; set; }
    }
}