using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace R7alaAPI.DTO
{
    public class ActivityCreateDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
        public decimal Price { get; set; } = 0m;

        public IFormFile? Thumbnail { get; set; }
    }

    public class ActivityUpdateDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Price cannot be negative.")]
        public decimal Price { get; set; } = 0m;

        public IFormFile? Thumbnail { get; set; }
    }

    public class ActivityDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int PlaceId { get; set; }
        public string ThumbnailUrl { get; set; }
    }
}