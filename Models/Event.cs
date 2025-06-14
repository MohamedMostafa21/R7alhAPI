using System;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required, MaxLength(100)]
        public string City { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees.")]
        public double Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Url]
        public string ThumbnailUrl { get; set; }
    }
}