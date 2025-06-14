using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Meal
    {
        public int Id { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        public Restaurant Restaurant { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, Range(0, double.MaxValue, ErrorMessage = "Price must be non-negative.")]
        public decimal Price { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Url]
        public string ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}