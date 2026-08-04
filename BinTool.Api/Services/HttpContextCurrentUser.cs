using System.Security.Claims;
using BinTool.Core.Services;

namespace BinTool.Api.Services;

/// <summary>
/// Reads the caller's identity off the validated bearer token for the current request.
/// </summary>
public class HttpContextCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public HttpContextCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public string? UserId => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string Name
    {
        get
        {
            var name = Principal?.Identity?.Name;
            return string.IsNullOrWhiteSpace(name) ? ICurrentUser.SystemName : name;
        }
    }

    private ClaimsPrincipal? Principal
    {
        get
        {
            var principal = _accessor.HttpContext?.User;
            return principal?.Identity?.IsAuthenticated == true ? principal : null;
        }
    }
}
