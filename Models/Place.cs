using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Place
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string Description { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } // Changed to string to match PlaceDto

        [Required, MaxLength(100)]
        public string City { get; set; }

        [Required, MaxLength(100)]
        public string Country { get; set; }

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Average price cannot be negative.")]
        public decimal? AveragePrice { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Url]
        public string? ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();

        public ICollection<Review>? Reviews { get; set; }
        public ICollection<Activity>? Activities { get; set; }
    }
}