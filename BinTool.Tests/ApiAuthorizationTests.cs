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

    /// <summary>
    /// BinRanges is the one controller that mixes access levels: its class-level rule
    /// admits both roles for reading, so each write has to raise the bar itself. Missing
    /// one would hand a viewer the ability to edit live BIN data.
    /// </summary>
    [Theory]
    [InlineData(nameof(BinRangesController.Create))]
    [InlineData(nameof(BinRangesController.Update))]
    [InlineData(nameof(BinRangesController.Delete))]
    [InlineData(nameof(BinRangesController.Restore))]
    public void Maintaining_a_single_range_requires_the_admin_role(string action)
    {
        var attribute = typeof(BinRangesController).GetMethod(action)!
            .GetCustomAttribute<AuthorizeAttribute>();

        attribute.Should().NotBeNull("{0} writes BIN data", action);
        attribute!.Roles.Should().Be(AppRoles.Admin);
    }

    [Theory]
    [InlineData(nameof(BinRangesController.Search))]
    [InlineData(nameof(BinRangesController.Filters))]
    [InlineData(nameof(BinRangesController.GetById))]
    public void Reading_a_range_is_not_narrowed_to_admin(string action)
    {
        typeof(BinRangesController).GetMethod(action)!
            .GetCustomAttribute<AuthorizeAttribute>()
            .Should().BeNull("a viewer must still be able to browse");
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
