using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.Models
{
    public enum ApplicationStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class TourGuideApplication
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
        [Url]
        [MaxLength(500)]
        public string CVUrl { get; set; }
        [Url]
        [MaxLength(500)]
        public string ProfilePictureUrl { get; set; }
        public ApplicationStatus Status { get; set; }
        [MaxLength(500)]
        public string? AdminComment { get; set; } // Nullable
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}