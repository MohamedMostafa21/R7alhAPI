using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public class TourGuide
    {
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        public User User { get; set; }
        [MaxLength(1000)]
        public string Bio { get; set; } = string.Empty;
        [Range(0, 50)]
        public int YearsOfExperience { get; set; }
        public List<string> Languages { get; set; } = new List<string>();
        [Range(0, double.MaxValue)]
        public decimal HourlyRate { get; set; }
        public bool IsAvailable { get; set; } = true;
        [Url]
        [MaxLength(500)]
        public string ProfilePictureUrl { get; set; }
        public List<Review> Reviews { get; set; } = new List<Review>();
        public List<Plan> Plans { get; set; } = new List<Plan>();
    }
}