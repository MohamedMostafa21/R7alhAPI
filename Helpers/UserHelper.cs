using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace R7alaAPI.Helpers
{
    public static class UserHelper
{
    // Get user ID from JWT claims
    public static string GetCurrentUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    // Extension method for ControllerBase
    public static string GetCurrentUserId(this ControllerBase controller)
    {
        var claim = controller.User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim == null)
            return null;
            
        return claim.Value;
    }
    
    // Check if user is authenticated
    public static bool IsAuthenticated(this ControllerBase controller)
    {
        return controller.User?.Identity?.IsAuthenticated ?? false;
    }

    // Get user email
    public static string GetCurrentUserEmail(this ControllerBase controller)
    {
        return controller.User.FindFirst(ClaimTypes.Email)?.Value;
    }
    
    // Get user roles
    public static IEnumerable<string> GetCurrentUserRoles(this ControllerBase controller)
    {
        return controller.User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value);
    }
}
}