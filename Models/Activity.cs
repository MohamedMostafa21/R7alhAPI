using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Activity
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string Description { get; set; } = string.Empty;

        [Url]
        public string? ThumbnailUrl { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
        public decimal Price { get; set; } = 0m;

        [Required]
        public int PlaceId { get; set; } // Foreign Key to Place

        public Place Place { get; set; } // Navigation property
    }
}