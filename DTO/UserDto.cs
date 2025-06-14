using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using R7alaAPI.Models;

// UserDto.cs
namespace R7alaAPI.DTO
{
    public class UserDto
    {
        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string FirstName { get; set; }
        public string LastName { get; set; }="";
        public Gender Gender { get; set; } = Gender.Male;


        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}

// UpdateUserDto.cs
namespace R7alaAPI.DTO
{
    public class UpdateUserDto
    {
        [MaxLength(20)]
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        
        [EmailAddress]
        public string? Email { get; set; }
        
        [Phone]
        public string? PhoneNumber { get; set; }
        
        public string? ProfilePictureUrl { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
    }
}