using System.Security.Claims;
using BinTool.Api.Services;
using BinTool.Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace BinTool.Tests;

/// <summary>
/// What gets stamped on audit fields for a given caller.
/// </summary>
public class CurrentUserTests
{
    private static HttpContextCurrentUser For(ClaimsPrincipal? user)
    {
        var context = new DefaultHttpContext();
        if (user is not null)
        {
            context.User = user;
        }

        return new HttpContextCurrentUser(new HttpContextAccessor { HttpContext = context });
    }

    private static ClaimsPrincipal SignedIn(string id, string name) =>
        new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, id), new Claim(ClaimTypes.Name, name) },
            authenticationType: "Test"));

    [Fact]
    public void An_authenticated_caller_is_recorded_by_name_and_id()
    {
        var current = For(SignedIn("user-1", "admin"));

        current.UserId.Should().Be("user-1");
        current.Name.Should().Be("admin");
    }

    [Fact]
    public void An_anonymous_caller_falls_back_to_the_system_identity()
    {
        // An identity with no authentication type is not authenticated, which is what
        // an unauthenticated request looks like.
        var current = For(new ClaimsPrincipal(new ClaimsIdentity()));

        current.UserId.Should().BeNull();
        current.Name.Should().Be(ICurrentUser.SystemName);
    }

    [Fact]
    public void Work_outside_a_request_is_recorded_as_system()
    {
        var current = new BinTool.Infrastructure.Services.SystemCurrentUser();

        current.UserId.Should().BeNull();
        current.Name.Should().Be("system");
    }
}
