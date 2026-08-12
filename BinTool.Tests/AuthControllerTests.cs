using BinTool.Api.Controllers;
using BinTool.Api.Services;
using BinTool.Application.Models.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace BinTool.Tests;

// What the endpoint makes of an outcome. The rules themselves are SignInServiceTests; what is left
// here is the mapping onto HTTP, including that a refusal never reaches the caller as a 200 with an
// empty token.
public class AuthControllerTests
{
    private static Task<IActionResult> Login(SignInOutcome outcome) =>
        new AuthController(
            new StubSignIn(outcome), new StubTokens(), NullLogger<AuthController>.Instance)
            .Login(new LoginRequest { UserName = "admin", Password = "whatever" }, default);

    private static LoginRejection Rejection(IActionResult result) =>
        result.Should().BeOfType<UnauthorizedObjectResult>()
            .Which.Value.Should().BeOfType<LoginRejection>().Which;

    [Fact]
    public async Task An_accepted_sign_in_returns_the_token_and_what_the_account_may_do()
    {
        var user = new AuthenticatedUser(
            "user-1", "admin", "admin@bintool.local", "System Administrator",
            new[] { "Admin" }, new[] { "binranges.read" });

        var result = await Login(SignInOutcome.Accepted(user));

        var response = result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<LoginResponse>().Which;

        response.AccessToken.Should().Be("stub-token");
        response.UserName.Should().Be("admin");
        response.Roles.Should().ContainSingle().Which.Should().Be("Admin");
        response.Permissions.Should().ContainSingle().Which.Should().Be("binranges.read");
    }

    [Fact]
    public async Task Rejected_credentials_come_back_as_401()
    {
        var result = await Login(SignInOutcome.Invalid());

        Rejection(result).Reason.Should().Be(LoginRejectionReason.InvalidCredentials);
    }

    [Fact]
    public async Task A_lockout_comes_back_as_401_with_the_time_it_lifts()
    {
        var until = DateTime.UtcNow.AddMinutes(3);

        var result = await Login(SignInOutcome.Locked(until));

        var rejection = Rejection(result);
        rejection.Reason.Should().Be(LoginRejectionReason.LockedOut);
        rejection.LockoutEndsUtc.Should().Be(until);
    }

    private sealed class StubSignIn : ISignInService
    {
        private readonly SignInOutcome _outcome;

        public StubSignIn(SignInOutcome outcome)
        {
            _outcome = outcome;
        }

        public Task<SignInOutcome> SignInAsync(
            string userName, string password, CancellationToken cancellationToken = default) =>
            Task.FromResult(_outcome);
    }

    private sealed class StubTokens : IJwtTokenService
    {
        public (string Token, DateTime ExpiresAtUtc) CreateToken(
            AuthenticatedUser user, IEnumerable<string> roles, IEnumerable<string> permissions)
            => ("stub-token", DateTime.UtcNow.AddMinutes(30));
    }
}
