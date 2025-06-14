using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace R7alaAPI.DTO
{
    public class PlaceDto
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; }

        [Required, MaxLength(50)]
        public string City { get; set; }

        [Required, MaxLength(50)]
        public string Country { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AveragePrice { get; set; } = 0.0m;

        public double Stars { get; set; } = 0.0d;

        public string ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();

        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }

        //[JsonIgnore]
        public List<ActivityDto> Activities { get; set; }

        public bool IsFavorited { get; set; }
    }

    public class PlaceCreateDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; }

        [Required, MaxLength(50)]
        public string City { get; set; }

        [Required, MaxLength(50)]
        public string Country { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AveragePrice { get; set; } = 0.0m;

        [Required]
        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }

    public class PlaceUpdateDto
    {
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public string Type { get; set; }

        [MaxLength(50)]
        public string City { get; set; }

        [MaxLength(50)]
        public string Country { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? AveragePrice { get; set; }

        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}