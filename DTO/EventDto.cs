using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R7alaAPI.DTO
{
    public class EventDto
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

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public string ThumbnailUrl { get; set; }
    }

    public class EventCreateDto
    {
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

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        public IFormFile Thumbnail { get; set; }
    }

    public class EventUpdateDto
    {
        [MaxLength(200)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        public IFormFile Thumbnail { get; set; }
    }
}