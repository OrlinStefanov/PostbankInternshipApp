using System.Reflection;
using BinTool.Api.Controllers;
using BinTool.Application.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Tests;

// The API's access rules, asserted from the attributes themselves. Authorization is easy to remove
// by accident and produces no failure when it goes missing - the endpoint simply starts answering
// everyone. These tests fail instead. Since the switch to permission-based authorization, the rule
// is a policy name (a permission key), not a role.
public class ApiAuthorizationTests
{
    private static AuthorizeAttribute? Authorize(Type controller) =>
        controller.GetCustomAttribute<AuthorizeAttribute>();

    private static AuthorizeAttribute? Authorize(Type controller, string action) =>
        controller.GetMethod(action)!.GetCustomAttribute<AuthorizeAttribute>();

    [Fact]
    public void Classifying_requires_the_classify_permission()
    {
        Authorize(typeof(BinController))!.Policy.Should().Be(Permissions.BinClassify);
    }

    [Fact]
    public void Importing_requires_the_import_permission()
    {
        Authorize(typeof(BinCsvImportController))!.Policy.Should().Be(Permissions.BinRangesImport);
    }

    [Fact]
    public void Managing_access_requires_the_roles_manage_permission()
    {
        Authorize(typeof(RolesController))!.Policy.Should().Be(Permissions.RolesManage);
        Authorize(typeof(UsersController))!.Policy.Should().Be(Permissions.RolesManage);
    }

    // BinRanges keeps a bare class-level rule - authenticated, no policy - so each action states
    // its own permission. Reads take the read permission; a missing one would hand an unprivileged
    // caller live BIN data.
    [Fact]
    public void The_bin_ranges_controller_requires_authentication_but_no_single_policy()
    {
        var attribute = Authorize(typeof(BinRangesController));

        attribute.Should().NotBeNull("browsing still requires authentication");
        attribute!.Policy.Should().BeNull("the per-action permissions decide access");
        attribute.Roles.Should().BeNull();
    }

    [Theory]
    [InlineData(nameof(BinRangesController.Create))]
    [InlineData(nameof(BinRangesController.Update))]
    [InlineData(nameof(BinRangesController.Delete))]
    [InlineData(nameof(BinRangesController.Restore))]
    public void Maintaining_a_single_range_requires_the_write_permission(string action)
    {
        Authorize(typeof(BinRangesController), action)!.Policy
            .Should().Be(Permissions.BinRangesWrite, "{0} writes BIN data", action);
    }

    [Theory]
    [InlineData(nameof(BinRangesController.Search))]
    [InlineData(nameof(BinRangesController.Filters))]
    [InlineData(nameof(BinRangesController.GetById))]
    public void Reading_a_range_requires_the_read_permission(string action)
    {
        Authorize(typeof(BinRangesController), action)!.Policy
            .Should().Be(Permissions.BinRangesRead, "{0} exposes BIN data", action);
    }

    [Fact]
    public void Reading_the_audit_log_requires_the_audit_read_permission()
    {
        Authorize(typeof(AuditController))!.Policy.Should().Be(Permissions.AuditRead);
    }

    [Theory]
    [InlineData(typeof(CardSchemesController))]
    [InlineData(typeof(ProductTypesController))]
    [InlineData(typeof(FundingTypesController))]
    [InlineData(typeof(RegionsController))]
    [InlineData(typeof(CountriesController))]
    public void Reference_data_controllers_require_the_reference_data_manage_permission(Type controller)
    {
        Authorize(controller)!.Policy.Should().Be(Permissions.ReferenceDataManage);
    }

    // Currencies are the one reference-data table whose reads are split off: pricing a lookup needs
    // the list, and a viewer holds no administration permission. A class-level manage policy would
    // put it out of their reach, and would not be overridden by the per-action one - authorize
    // attributes stack.
    [Fact]
    public void The_currencies_controller_requires_authentication_but_no_single_policy()
    {
        var attribute = Authorize(typeof(CurrenciesController));

        attribute.Should().NotBeNull("the currency list is not public");
        attribute!.Policy.Should().BeNull("the per-action permissions decide access");
        attribute.Roles.Should().BeNull();
    }

    [Theory]
    [InlineData(nameof(CurrenciesController.Search))]
    [InlineData(nameof(CurrenciesController.GetById))]
    public void Reading_a_currency_requires_only_the_currencies_read_permission(string action)
    {
        Authorize(typeof(CurrenciesController), action)!.Policy
            .Should().Be(Permissions.CurrenciesRead, "{0} is needed to price a lookup", action);
    }

    [Theory]
    [InlineData(nameof(CurrenciesController.Create))]
    [InlineData(nameof(CurrenciesController.Update))]
    [InlineData(nameof(CurrenciesController.Delete))]
    [InlineData(nameof(CurrenciesController.Restore))]
    public void Maintaining_a_currency_requires_the_reference_data_manage_permission(string action)
    {
        Authorize(typeof(CurrenciesController), action)!.Policy
            .Should().Be(Permissions.ReferenceDataManage, "{0} changes reference data", action);
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
