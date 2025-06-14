using Microsoft.AspNetCore.Http;
using R7alaAPI.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.DTO
{
    public class TourGuideApplicationDto
    {
        [Required]
        [MaxLength(1000)]
        public string Bio { get; set; }
        [Required]
        [Range(0, 50)]
        public int YearsOfExperience { get; set; }
        [Required]
        public List<string> Languages { get; set; } = new List<string>();
        [Range(0, double.MaxValue)]
        public decimal HourlyRate { get; set; }
        [Required]
        public IFormFile CV { get; set; }
        public IFormFile ProfilePicture { get; set; }
    }

    public class TourGuideApplicationResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Bio { get; set; }
        public int YearsOfExperience { get; set; }
        public List<string> Languages { get; set; }
        public decimal HourlyRate { get; set; }
        public string CVUrl { get; set; }
        public string ProfilePictureUrl { get; set; }
        public ApplicationStatus Status { get; set; }
        public string AdminComment { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }

    public class TourGuideDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        [MaxLength(1000)]
        public string Bio { get; set; }
        [Range(0, 50)]
        public int YearsOfExperience { get; set; }
        public List<string> Languages { get; set; }
        [Range(0, double.MaxValue)]
        public decimal HourlyRate { get; set; }
        public string? City { get; set; }
        public bool IsAvailable { get; set; }
        public string ProfilePictureUrl { get; set; }
        public double Stars { get; set; }
        public bool IsFavorited { get; set; } = false;
    }

    public class AdminCommentDto
    {
        [MaxLength(500)]
        public string Comment { get; set; }
    }
}