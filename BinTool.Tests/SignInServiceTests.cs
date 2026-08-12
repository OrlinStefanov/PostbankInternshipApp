using BinTool.Application.Authorization;
using BinTool.Application.Models.Auth;
using BinTool.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BinTool.Tests;

// How sign-in treats passwords and lockouts against a real UserManager: the password is checked
// before the lock, so the account's owner is never shut out by their own typos, and a lockout is
// reported distinctly from a wrong password.
public class SignInServiceTests : IdentityTestBase
{
    private SignInService CreateService() =>
        new(new CredentialStore(Users),
            new RoleRepository(Roles, Users, Db),
            NullLogger<SignInService>.Instance);

    private Task<SignInOutcome> SignIn(string userName, string password) =>
        CreateService().SignInAsync(userName, password);

    [Fact]
    public async Task The_correct_password_signs_in()
    {
        await CreateUserAsync("admin", AppRoles.Admin);

        var outcome = await SignIn("admin", "Passw0rd!");

        outcome.User.Should().NotBeNull();
        outcome.User!.UserName.Should().Be("admin");
        outcome.Reason.Should().BeNull();
    }

    [Fact]
    public async Task An_admin_holds_every_permission_in_the_catalog()
    {
        await CreateUserAsync("admin", AppRoles.Admin);

        var outcome = await SignIn("admin", "Passw0rd!");

        outcome.User!.Permissions.Should().BeEquivalentTo(Permissions.AllKeys);
    }

    [Fact]
    public async Task A_wrong_password_is_rejected_as_invalid_credentials()
    {
        await CreateUserAsync("admin", AppRoles.Admin);

        var outcome = await SignIn("admin", "wrong");

        outcome.User.Should().BeNull();
        outcome.Reason.Should().Be(LoginRejectionReason.InvalidCredentials);
    }

    [Fact]
    public async Task An_unknown_user_is_rejected_as_invalid_credentials()
    {
        var outcome = await SignIn("nobody", "whatever");

        outcome.Reason.Should().Be(LoginRejectionReason.InvalidCredentials);
    }

    [Fact]
    public async Task A_deactivated_account_is_rejected_as_invalid_credentials()
    {
        var user = await CreateUserAsync("admin", AppRoles.Admin);
        user.IsActive = false;
        await Users.UpdateAsync(user);

        var outcome = await SignIn("admin", "Passw0rd!");

        outcome.Reason.Should().Be(LoginRejectionReason.InvalidCredentials);
    }

    [Fact]
    public async Task Five_wrong_attempts_lock_the_account_and_say_so()
    {
        await CreateUserAsync("admin", AppRoles.Admin);

        SignInOutcome outcome = null!;
        for (var i = 0; i < 5; i++)
        {
            outcome = await SignIn("admin", "wrong");
        }

        outcome.Reason.Should().Be(LoginRejectionReason.LockedOut);
        outcome.LockoutEndsUtc.Should().NotBeNull().And.Subject.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task The_correct_password_releases_an_active_lockout()
    {
        await CreateUserAsync("admin", AppRoles.Admin);
        for (var i = 0; i < 5; i++)
        {
            await SignIn("admin", "wrong");
        }

        (await Users.IsLockedOutAsync((await Users.FindByNameAsync("admin"))!)).Should().BeTrue();

        var outcome = await SignIn("admin", "Passw0rd!");

        outcome.User.Should().NotBeNull();

        var reloaded = (await Users.FindByNameAsync("admin"))!;
        (await Users.IsLockedOutAsync(reloaded)).Should().BeFalse();
        (await Users.GetAccessFailedCountAsync(reloaded)).Should().Be(0);
    }

    [Fact]
    public async Task A_successful_sign_in_clears_a_partial_failed_count()
    {
        await CreateUserAsync("admin", AppRoles.Admin);
        await SignIn("admin", "wrong");
        await SignIn("admin", "wrong");

        await SignIn("admin", "Passw0rd!");

        var reloaded = (await Users.FindByNameAsync("admin"))!;
        (await Users.GetAccessFailedCountAsync(reloaded)).Should().Be(0);
    }

    [Fact]
    public async Task A_successful_sign_in_stamps_the_last_login()
    {
        await CreateUserAsync("admin", AppRoles.Admin);

        await SignIn("admin", "Passw0rd!");

        var reloaded = (await Users.FindByNameAsync("admin"))!;
        reloaded.LastLoginAt.Should().NotBeNull().And.Subject.Should().BeCloseTo(
            DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
}
