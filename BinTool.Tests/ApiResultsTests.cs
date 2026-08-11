using BinTool.Api.Errors;
using BinTool.Application.Models.Access;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Commission;
using BinTool.Application.Models.Common;
using BinTool.Application.Models.Currency;
using BinTool.Application.Models.ReferenceData;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Tests;

// How a refused write becomes a status code. This used to be a switch per controller, and the
// failure mode it invites is silent: a status added to an enum but left out of one switch falls
// through to Ok, so the API answers 200 to a write it refused. The last theory here is the guard
// against that - it walks every value of every status enum rather than the ones someone
// remembered to list.
public class ApiResultsTests
{
    [Theory]
    [InlineData(MutationOutcome.Succeeded, typeof(OkObjectResult))]
    [InlineData(MutationOutcome.Invalid, typeof(BadRequestObjectResult))]
    [InlineData(MutationOutcome.NotFound, typeof(NotFoundObjectResult))]
    [InlineData(MutationOutcome.Conflict, typeof(ConflictObjectResult))]
    public void Each_outcome_maps_to_its_status_code(MutationOutcome outcome, Type expected)
    {
        var result = ApiResults.From(new StubResult(outcome));

        result.Should().BeOfType(expected);
    }

    [Fact]
    public void The_result_itself_is_the_body_on_a_refusal()
    {
        var refused = BinRangeMutationResult.Failure(
            BinRangeMutationStatus.PrefixInUse, "400001 already exists.");

        var body = ApiResults.From(refused).Should().BeOfType<ConflictObjectResult>()
            .Which.Value;

        // The caller reads one shape either way, and the reason is on it.
        body.Should().BeSameAs(refused);
        refused.Error.Should().Be("400001 already exists.");
    }

    [Theory]
    [MemberData(nameof(EveryStatusValue))]
    public void Only_a_named_success_maps_to_a_200(string enumName, string valueName, MutationOutcome outcome)
    {
        var isNamedSuccess = valueName is "Created" or "Updated" or "Deleted" or "Restored";

        (outcome == MutationOutcome.Succeeded).Should().Be(isNamedSuccess,
            $"{enumName}.{valueName} is {(isNamedSuccess ? "" : "not ")}a success, " +
            "so a caller must {0} be told the write happened",
            isNamedSuccess ? "" : "not");
    }

    public static TheoryData<string, string, MutationOutcome> EveryStatusValue()
    {
        var data = new TheoryData<string, string, MutationOutcome>();

        Add<BinRangeMutationStatus>(s => s.Outcome());
        Add<LookupMutationStatus>(s => s.Outcome());
        Add<CommissionRuleMutationStatus>(s => s.Outcome());
        Add<RoleMutationStatus>(s => s.Outcome());
        Add<UserRolesStatus>(s => new UserRolesResult { Status = s }.Outcome);

        return data;

        void Add<TStatus>(Func<TStatus, MutationOutcome> outcome) where TStatus : struct, Enum
        {
            foreach (var value in Enum.GetValues<TStatus>())
            {
                data.Add(typeof(TStatus).Name, value.ToString()!, outcome(value));
            }
        }
    }

    private sealed record StubResult(MutationOutcome Outcome) : IMutationResult
    {
        public string? Error => null;
    }
}
