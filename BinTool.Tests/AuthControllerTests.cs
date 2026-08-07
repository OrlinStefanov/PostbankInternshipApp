using BinTool.Api.Controllers;
using BinTool.Api.Services;
using BinTool.Core.Authorization;
using BinTool.Core.Entities;
using BinTool.Core.Models.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace BinTool.Tests;

/// <summary>
/// How <see cref="AuthController.Login"/> treats passwords and lockouts against a real
/// <see cref="Microsoft.AspNetCore.Identity.UserManager{T}"/>: the password is checked before
/// the lock, so the account's owner is never shut out by their own typos, and a lockout is
/// reported distinctly from a wrong password.
/// </summary>
public class AuthControllerTests : IdentityTestBase
{
    private AuthController CreateController() =>
        new(Users, Roles, new StubTokens(), NullLogger<AuthController>.Instance);

    private Task<IActionResult> Login(string userName, string password) =>
        CreateController().Login(new LoginRequest { UserName = userName, Password = password });

    private static LoginRejection Rejection(IActionResult result) =>
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().BeOfType<LoginRejection>().Which;

    [Fact]
    public async Task The_correct_password_signs_in()
    {
        await CreateUserAsync("admin", AppRoles.Admin);

        var result = await Login("admin", "Passw0rd!");

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<LoginResponse>();
    }

    [Fact]
    public async Task A_wrong_password_is_rejected_as_invalid_credentials()
    {
        await CreateUserAsync("admin", AppRoles.Admin);

        var result = await Login("admin", "wrong");

        Rejection(result).Reason.Should().Be(LoginRejectionReason.InvalidCredentials);
    }

    [Fact]
    public async Task An_unknown_user_is_rejected_as_invalid_credentials()
    {
        var result = await Login("nobody", "whatever");

        Rejection(result).Reason.Should().Be(LoginRejectionReason.InvalidCredentials);
    }

    [Fact]
    public async Task A_deactivated_account_is_rejected_as_invalid_credentials()
    {
        var user = await CreateUserAsync("admin", AppRoles.Admin);
        user.IsActive = false;
        await Users.UpdateAsync(user);

        var result = await Login("admin", "Passw0rd!");

        Rejection(result).Reason.Should().Be(LoginRejectionReason.InvalidCredentials);
    }

    [Fact]
    public async Task Five_wrong_attempts_lock_the_account_and_say_so()
    {
        await CreateUserAsync("admin", AppRoles.Admin);

        IActionResult result = null!;
        for (var i = 0; i < 5; i++)
        {
            result = await Login("admin", "wrong");
        }

        var rejection = Rejection(result);
        rejection.Reason.Should().Be(LoginRejectionReason.LockedOut);
        rejection.LockoutEndsUtc.Should().NotBeNull().And.Subject.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task The_correct_password_releases_an_active_lockout()
    {
        await CreateUserAsync("admin", AppRoles.Admin);
        for (var i = 0; i < 5; i++)
        {
            await Login("admin", "wrong");
        }

        (await Users.IsLockedOutAsync((await Users.FindByNameAsync("admin"))!)).Should().BeTrue();

        var result = await Login("admin", "Passw0rd!");

        result.Should().BeOfType<OkObjectResult>();

        var reloaded = (await Users.FindByNameAsync("admin"))!;
        (await Users.IsLockedOutAsync(reloaded)).Should().BeFalse();
        (await Users.GetAccessFailedCountAsync(reloaded)).Should().Be(0);
    }

    [Fact]
    public async Task A_successful_sign_in_clears_a_partial_failed_count()
    {
        await CreateUserAsync("admin", AppRoles.Admin);
        await Login("admin", "wrong");
        await Login("admin", "wrong");

        await Login("admin", "Passw0rd!");

        var reloaded = (await Users.FindByNameAsync("admin"))!;
        (await Users.GetAccessFailedCountAsync(reloaded)).Should().Be(0);
    }

    private sealed class StubTokens : IJwtTokenService
    {
        public (string Token, DateTime ExpiresAtUtc) CreateToken(
            ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions)
            => ("stub-token", DateTime.UtcNow.AddMinutes(30));
    }
}
