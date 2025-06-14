using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R7alaAPI.DTO
{
    public class MealDto
    {
        public int Id { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public string ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class MealCreateDto
    {
        [Required]
        public int RestaurantId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }

    public class MealUpdateDto
    {
        [MaxLength(100)]
        public string Name { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}