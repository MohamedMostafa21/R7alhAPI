using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class Room
    {
        public int Id { get; set; }

        [Required]
        public int HotelId { get; set; }

        public Hotel Hotel { get; set; }

        [Required, MaxLength(50)]
        public string Type { get; set; } // e.g., Single, Double, Suite

        [Required, Range(1, int.MaxValue, ErrorMessage = "Capacity must be at least 1.")]
        public int Capacity { get; set; }

        [Required, Range(0, double.MaxValue, ErrorMessage = "Price per night must be non-negative.")]
        public decimal PricePerNight { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public bool IsAvailable { get; set; } = true;

        [Url]
        public string ThumbnailUrl { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }
}