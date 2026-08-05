using BinTool.Core.Entities;
using BinTool.Core.Models.Access;
using BinTool.Infrastructure.Services;
using FluentAssertions;

namespace BinTool.Tests;

/// <summary>
/// Assigning roles to users, and the two rules that protect the god account: the last admin
/// keeps Admin, and no one strips their own.
/// </summary>
public class UserAdminServiceTests : IdentityTestBase
{
    private UserAdminService NewService() =>
        new(Users, Roles, CurrentUser, Audit(), Db);

    private static UserRolesInput Want(params string[] roles) =>
        new() { Roles = roles.ToList() };

    [Fact]
    public async Task Set_roles_assigns_and_removes_to_match_the_request()
    {
        var user = await CreateUserAsync("dana", AppRoles.Viewer);

        var result = await NewService().SetRolesAsync(user.Id, Want(AppRoles.Admin));

        result.Status.Should().Be(UserRolesStatus.Updated);
        result.User!.Roles.Should().BeEquivalentTo(new[] { AppRoles.Admin });
    }

    [Fact]
    public async Task Removing_the_last_admin_is_refused()
    {
        var onlyAdmin = await CreateUserAsync("root", AppRoles.Admin);
        CurrentUser.UserId = "someone-else"; // not self, so the last-admin rule is what fires

        var result = await NewService().SetRolesAsync(onlyAdmin.Id, Want(AppRoles.Viewer));

        result.Status.Should().Be(UserRolesStatus.LastAdmin);
    }

    [Fact]
    public async Task Removing_your_own_admin_is_refused()
    {
        // Two admins, so the last-admin rule does not apply - self-demotion is the reason.
        var me = await CreateUserAsync("me", AppRoles.Admin);
        await CreateUserAsync("other-admin", AppRoles.Admin);
        CurrentUser.UserId = me.Id;

        var result = await NewService().SetRolesAsync(me.Id, Want(AppRoles.Viewer));

        result.Status.Should().Be(UserRolesStatus.SelfDemotion);
    }

    [Fact]
    public async Task Another_admin_can_be_demoted_while_one_remains()
    {
        var actor = await CreateUserAsync("actor", AppRoles.Admin);
        var target = await CreateUserAsync("target", AppRoles.Admin);
        CurrentUser.UserId = actor.Id;

        var result = await NewService().SetRolesAsync(target.Id, Want(AppRoles.Viewer));

        result.Status.Should().Be(UserRolesStatus.Updated);
        result.User!.Roles.Should().BeEquivalentTo(new[] { AppRoles.Viewer });
    }

    [Fact]
    public async Task An_unknown_role_is_rejected()
    {
        var user = await CreateUserAsync("dana", AppRoles.Viewer);

        var result = await NewService().SetRolesAsync(user.Id, Want("NoSuchRole"));

        result.Status.Should().Be(UserRolesStatus.UnknownRole);
    }

    [Fact]
    public async Task An_unknown_user_is_not_found()
    {
        var result = await NewService().SetRolesAsync("missing", Want(AppRoles.Viewer));

        result.Status.Should().Be(UserRolesStatus.NotFound);
    }
}
