using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R7alaAPI.DTO
{
    public class RoomDto
    {
        public int Id { get; set; }

        [Required]
        public int HotelId { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal PricePerNight { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        public string ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class RoomCreateDto
    {

        [Required, MaxLength(50)]
        public string Type { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int Capacity { get; set; }

        [Required, Range(0, double.MaxValue)]
        public decimal PricePerNight { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        public bool IsAvailable { get; set; } = true;


        [Required]
        public IFormFile Thumbnail { get; set; }

        public List<IFormFile> Images { get; set; }
    }

    public class RoomUpdateDto
    {
        [MaxLength(50)]
        public string Type { get; set; }

        [Range(1, int.MaxValue)]
        public int? Capacity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PricePerNight { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        public bool? IsAvailable { get; set; }

        public IFormFile Thumbnail { get; set; }
        public string? ThumbnailUrl { get; set; }
        public bool isAvilable { get; set; } = false;

        public List<IFormFile> Images { get; set; }
    }
}