using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace R7alaAPI.Models
{
    public class User : IdentityUser<int>
    {
        [Required, MaxLength(20)]
        public string FirstName { get; set; }

        [MaxLength(20)]
        public string LastName { get; set; } = "";

        [Required]
        public Gender Gender { get; set; } = Gender.Male;

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Url]
        public string ProfilePictureUrl { get; set; }

        [MaxLength(100)]
        public string Country { get; set; }

        [MaxLength(100)]
        public string City { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public TourGuide TourGuide { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
        public ICollection<Review> Reviews { get; set; }
    }

    public enum Gender
    {
        Female,
        Male
    }
}