using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R7alaAPI.DTO
{
    public class CityDto
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(100)]
        public string Country { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class CityCreateDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(100)]
        public string Country { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }

    public class CityUpdateDto
    {
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string Country { get; set; }

        [MaxLength(1000)]
        public string Description { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }
}