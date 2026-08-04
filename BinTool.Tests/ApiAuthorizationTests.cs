using System.Reflection;
using BinTool.Api.Controllers;
using BinTool.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Tests;

/// <summary>
/// The API's access rules, asserted from the attributes themselves.
/// <para>
/// Authorization is easy to remove by accident and produces no failure when it goes
/// missing - the endpoint simply starts answering everyone. These tests fail instead.
/// </para>
/// </summary>
public class ApiAuthorizationTests
{
    private static AuthorizeAttribute? Authorize(Type controller) =>
        controller.GetCustomAttribute<AuthorizeAttribute>();

    [Fact]
    public void Importing_requires_the_admin_role()
    {
        Authorize(typeof(BinCsvImportController))!.Roles.Should().Be(AppRoles.Admin);
    }

    [Theory]
    [InlineData(typeof(BinController))]
    [InlineData(typeof(BinRangesController))]
    public void Read_only_endpoints_require_a_signed_in_user_of_any_role(Type controller)
    {
        var attribute = Authorize(controller);

        attribute.Should().NotBeNull("reading still requires authentication");
        attribute!.Roles.Should().BeNull("both roles may read");
    }

    [Fact]
    public void Health_stays_anonymous()
    {
        typeof(HealthController).GetCustomAttribute<AllowAnonymousAttribute>()
            .Should().NotBeNull("a health probe cannot hold a token");
    }

    [Fact]
    public void Login_is_anonymous_but_the_rest_of_auth_is_not()
    {
        var login = typeof(AuthController).GetMethod(nameof(AuthController.Login))!;
        var me = typeof(AuthController).GetMethod(nameof(AuthController.Me))!;

        login.GetCustomAttribute<AllowAnonymousAttribute>()
            .Should().NotBeNull("you cannot hold a token before logging in");
        me.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();
    }

    [Fact]
    public void Every_controller_states_its_access_rule()
    {
        var controllers = typeof(BinController).Assembly
            .GetTypes()
            .Where(t => t.IsAssignableTo(typeof(ControllerBase)) && !t.IsAbstract);

        foreach (var controller in controllers)
        {
            var declared = controller.GetCustomAttribute<AuthorizeAttribute>() is not null
                || controller.GetCustomAttribute<AllowAnonymousAttribute>() is not null;

            declared.Should().BeTrue(
                "{0} must be explicitly authorized or explicitly anonymous, never neither",
                controller.Name);
        }
    }
}
