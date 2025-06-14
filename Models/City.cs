using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class City
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(100)]
        public string Country { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }

        [Url]
        public string ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}