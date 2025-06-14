using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R7alaAPI.DTO
{
    public class RestaurantDto
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
        public string Location { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required, MaxLength(50)]
        public string Cuisine { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AveragePrice { get; set; }

        public double Stars { get; set; } = 0.0d;

        public string ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();

        public List<MealDto> Meals { get; set; } = new List<MealDto>();

        public bool IsFavorited { get; set; }
    }

    public class RestaurantCreateDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

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

        [Required, MaxLength(50)]
        public string Cuisine { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AveragePrice { get; set; }

        [Required]
        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }

    public class RestaurantUpdateDto
    {
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        [MaxLength(50)]
        public string City { get; set; }

        [MaxLength(50)]
        public string Country { get; set; }

        [MaxLength(200)]
        public string Location { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        [MaxLength(50)]
        public string Cuisine { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? AveragePrice { get; set; }

        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}