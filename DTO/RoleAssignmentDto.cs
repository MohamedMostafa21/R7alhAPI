using System.ComponentModel.DataAnnotations;

namespace R7alaAPI.DTO
{
    public class RoleAssignmentDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string RoleName { get; set; }
    }
}