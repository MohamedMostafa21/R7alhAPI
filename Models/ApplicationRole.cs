using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace R7alaAPI.Models
{
    public class ApplicationRole : IdentityRole<int>
    {
        public ApplicationRole() : base() { }
        
        public ApplicationRole(string roleName) : base(roleName)
        {
            Name = roleName;
        }
    }
}