using BinTool.Application.Models.Auth;
using FluentAssertions;

namespace BinTool.Tests;

/// <summary>
/// The wording of a rejected sign-in.
/// <para>
/// Lockout is the reason a correct password can start being refused, and the response is
/// forbidden from saying which account it happened to. If the message stops mentioning
/// lockout, the only remaining clue is the server log - and the person signing in
/// concludes their password is wrong and keeps retrying a lock that only time clears.
/// </para>
/// </summary>
public class LoginRejectionTests
{
    [Fact]
    public void The_rejection_message_names_lockout_as_a_possible_cause()
    {
        LoginMessages.Rejected.Should().ContainEquivalentOf("locked");
    }

    [Fact]
    public void The_rejection_message_does_not_say_which_cause_applied()
    {
        // Anything definite here would confirm whether an account exists.
        LoginMessages.Rejected.Should().NotContainEquivalentOf("no such user");
        LoginMessages.Rejected.Should().NotContainEquivalentOf("does not exist");
        LoginMessages.Rejected.Should().NotContainEquivalentOf("deactivated");
    }

    [Fact]
    public void The_lockout_message_names_lockout_and_the_wait()
    {
        var message = LoginMessages.LockedOut(3);

        message.Should().ContainEquivalentOf("locked");
        message.Should().Contain("3 minutes");
    }

    [Fact]
    public void The_lockout_message_reads_naturally_for_a_single_minute()
    {
        LoginMessages.LockedOut(1).Should().Contain("a minute");
        LoginMessages.LockedOut(1).Should().NotContain("1 minutes");
    }
}
