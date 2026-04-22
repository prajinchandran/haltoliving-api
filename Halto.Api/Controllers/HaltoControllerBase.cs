using System.Security.Claims;
using Halto.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Halto.Api.Controllers;

[ApiController]
public abstract class HaltoControllerBase : ControllerBase
{
    protected Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token."));

    protected Guid CurrentOrgId
    {
        get
        {
            var orgClaim = User.FindFirstValue("org");
            if (string.IsNullOrEmpty(orgClaim))
                throw new UnauthorizedAccessException("Organization ID not found in token.");
            return Guid.Parse(orgClaim);
        }
    }

    protected string CurrentRole =>
        User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    protected bool IsSuperAdmin =>
        CurrentRole == "SuperAdmin";

    protected IActionResult ToActionResult<T>(Result<T> result)
    {
        if (!result.Succeeded)
        {
            return result.StatusCode switch
            {
                404 => NotFound(new { success = false, error = result.Error }),
                403 => Forbid(),
                401 => Unauthorized(new { success = false, error = result.Error }),
                _ => BadRequest(new { success = false, error = result.Error })
            };
        }

        return result.StatusCode == 201
            ? StatusCode(201, new { success = true, data = result.Data })
            : Ok(new { success = true, data = result.Data });
    }
}
