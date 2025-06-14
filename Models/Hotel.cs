using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Hotel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string City { get; set; }

        [Required, MaxLength(50)]
        public string Country { get; set; }

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public decimal StartingPrice { get; set; }

        [Range(0, 5)]
        public decimal Rate { get; set; }

        [Url]
        public string? ThumbnailUrl { get; set; }

        public List<string>? ImageUrls { get; set; } = new List<string>();

        public List<Room>? Rooms { get; set; } = new List<Room>();

        public List<Review>? Reviews { get; set; } = new List<Review>();
    }
}