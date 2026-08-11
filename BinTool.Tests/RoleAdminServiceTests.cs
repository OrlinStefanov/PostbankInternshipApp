using BinTool.Application.Authorization;
using BinTool.Domain.Entities;
using BinTool.Application.Models.Access;
using BinTool.Infrastructure.Services;
using FluentAssertions;

namespace BinTool.Tests;

/// <summary>
/// Creating, editing and deleting roles - and the guardrails that keep the Admin role a god.
/// </summary>
public class RoleAdminServiceTests : IdentityTestBase
{
    private RoleAdminService NewService() =>
        new(Roles, Users, CurrentUser, Audit(), Db);

    private static RoleInput Input(string name, string? description = null, params string[] permissions) =>
        new() { Name = name, Description = description, Permissions = permissions.ToList() };

    [Fact]
    public async Task Create_adds_a_role_with_its_permissions()
    {
        var result = await NewService().CreateAsync(
            Input("Analyst", "Reads and edits", Permissions.BinRangesRead, Permissions.BinRangesWrite));

        result.Status.Should().Be(RoleMutationStatus.Created);
        result.Role!.Name.Should().Be("Analyst");
        result.Role.Permissions.Should().BeEquivalentTo(
            new[] { Permissions.BinRangesRead, Permissions.BinRangesWrite });
        result.Role.IsProtected.Should().BeFalse();
    }

    [Fact]
    public async Task Create_rejects_a_name_already_taken()
    {
        var result = await NewService().CreateAsync(Input(AppRoles.Viewer));

        result.Status.Should().Be(RoleMutationStatus.NameInUse);
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Update_changes_the_name_description_and_permissions()
    {
        var created = await NewService().CreateAsync(Input("Analyst", "old", Permissions.BinRangesRead));

        var result = await NewService().UpdateAsync(
            created.Role!.Id, Input("Reviewer", "new", Permissions.BinRangesImport));

        result.Status.Should().Be(RoleMutationStatus.Updated);
        result.Role!.Name.Should().Be("Reviewer");
        result.Role.Description.Should().Be("new");
        result.Role.Permissions.Should().BeEquivalentTo(new[] { Permissions.BinRangesImport });
    }

    [Fact]
    public async Task Update_refuses_the_protected_admin_role()
    {
        var admin = await NewService().GetRolesAsync();
        var adminId = admin.Single(r => r.Name == AppRoles.Admin).Id;

        var result = await NewService().UpdateAsync(adminId, Input("Overlord"));

        result.Status.Should().Be(RoleMutationStatus.Protected);
    }

    [Fact]
    public async Task Delete_removes_an_empty_role()
    {
        var created = await NewService().CreateAsync(Input("Temp"));

        var result = await NewService().DeleteAsync(created.Role!.Id);

        result.Status.Should().Be(RoleMutationStatus.Deleted);
        (await NewService().GetRolesAsync()).Should().NotContain(r => r.Name == "Temp");
    }

    [Fact]
    public async Task Delete_refuses_a_role_that_still_has_members()
    {
        var created = await NewService().CreateAsync(Input("Analyst", null, Permissions.BinRangesRead));
        await CreateUserAsync("holder", "Analyst");

        var result = await NewService().DeleteAsync(created.Role!.Id);

        result.Status.Should().Be(RoleMutationStatus.InUse);
    }

    [Fact]
    public async Task Delete_refuses_the_protected_admin_role()
    {
        var adminId = (await NewService().GetRolesAsync()).Single(r => r.Name == AppRoles.Admin).Id;

        var result = await NewService().DeleteAsync(adminId);

        result.Status.Should().Be(RoleMutationStatus.Protected);
    }

    [Fact]
    public async Task Get_reports_member_counts_and_marks_admin_protected()
    {
        await CreateUserAsync("a", AppRoles.Admin);
        await CreateUserAsync("b", AppRoles.Viewer);
        await CreateUserAsync("c", AppRoles.Viewer);

        var roles = await NewService().GetRolesAsync();

        var admin = roles.Single(r => r.Name == AppRoles.Admin);
        admin.IsProtected.Should().BeTrue();
        admin.MemberCount.Should().Be(1);
        roles.Single(r => r.Name == AppRoles.Viewer).MemberCount.Should().Be(2);

        // The protected role sorts to the top.
        roles.First().IsProtected.Should().BeTrue();
    }
}
