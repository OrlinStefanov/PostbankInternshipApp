using BinTool.Domain.Common;
using FluentAssertions;

namespace BinTool.Tests;

public class RuleCriteriaKeyTests
{
    private const int Visa = 1, Mastercard = 2;
    private const int Consumer = 1, Commercial = 2;
    private const int Credit = 1, Debit = 2;
    private const int Domestic = 1, International = 2;

    // ---- Suggested score --------------------------------------------------------

    [Fact]
    public void An_all_wildcard_key_suggests_zero()
    {
        new RuleCriteriaKey(null, null, null, null).SuggestedPriorityScore.Should().Be(0);
    }

    [Fact]
    public void Each_pinned_field_adds_one()
    {
        new RuleCriteriaKey(Visa, Consumer, null, null).SuggestedPriorityScore.Should().Be(2);
    }

    [Fact]
    public void A_fully_pinned_key_suggests_the_field_count()
    {
        new RuleCriteriaKey(Visa, Consumer, Credit, Domestic)
            .SuggestedPriorityScore.Should().Be(RuleCriteriaKey.FieldCount);
    }

    [Fact]
    public void Only_an_all_wildcard_key_is_a_wildcard()
    {
        new RuleCriteriaKey(null, null, null, null).IsWildcard.Should().BeTrue();
        new RuleCriteriaKey(null, null, null, Domestic).IsWildcard.Should().BeFalse();
    }

    // ---- Matching a card --------------------------------------------------------

    [Fact]
    public void A_wildcard_key_matches_any_card()
    {
        new RuleCriteriaKey(null, null, null, null)
            .Matches(Visa, Consumer, Credit, Domestic).Should().BeTrue();
    }

    [Fact]
    public void A_pinned_field_must_agree_with_the_card()
    {
        var key = new RuleCriteriaKey(Visa, null, null, null);

        key.Matches(Visa, Consumer, Credit, Domestic).Should().BeTrue();
        key.Matches(Mastercard, Consumer, Credit, Domestic).Should().BeFalse();
    }

    // ---- Co-matchability: the ambiguity rule ------------------------------------

    [Fact]
    public void Keys_that_disagree_on_a_pinned_field_are_not_co_matchable()
    {
        // No card is both Visa and Mastercard, so these two can never collide however they
        // are ranked - which is why saving both at the same priority has to be allowed.
        new RuleCriteriaKey(Visa, null, null, null)
            .IsCoMatchableWith(new RuleCriteriaKey(Mastercard, null, null, null))
            .Should().BeFalse();
    }

    [Fact]
    public void Different_keys_can_still_describe_the_same_card()
    {
        // (Visa, Consumer, *, *) and (Visa, *, Credit, *) are different keys, but a Visa
        // consumer credit card matches both. This is the case equality would miss.
        new RuleCriteriaKey(Visa, Consumer, null, null)
            .IsCoMatchableWith(new RuleCriteriaKey(Visa, null, Credit, null))
            .Should().BeTrue();
    }

    [Fact]
    public void A_wildcard_key_is_co_matchable_with_everything()
    {
        new RuleCriteriaKey(null, null, null, null)
            .IsCoMatchableWith(new RuleCriteriaKey(Visa, Consumer, Credit, Domestic))
            .Should().BeTrue();
    }

    [Fact]
    public void A_key_is_co_matchable_with_itself()
    {
        var key = new RuleCriteriaKey(Visa, Consumer, Credit, Domestic);

        key.IsCoMatchableWith(key).Should().BeTrue();
    }

    [Fact]
    public void Co_matchability_is_symmetric()
    {
        var a = new RuleCriteriaKey(Visa, Consumer, null, null);
        var b = new RuleCriteriaKey(Visa, null, Credit, null);

        a.IsCoMatchableWith(b).Should().Be(b.IsCoMatchableWith(a));
    }

    [Fact]
    public void One_disagreeing_field_is_enough_to_rule_out_a_collision()
    {
        // Agreeing on three fields does not matter if the fourth cannot both be true.
        new RuleCriteriaKey(Visa, Consumer, Credit, Domestic)
            .IsCoMatchableWith(new RuleCriteriaKey(Visa, Consumer, Credit, International))
            .Should().BeFalse();
    }

    [Theory]
    [InlineData(Commercial, false)]
    [InlineData(Consumer, true)]
    public void Agreement_is_decided_field_by_field(int otherProduct, bool expected)
    {
        new RuleCriteriaKey(Visa, Consumer, null, null)
            .IsCoMatchableWith(new RuleCriteriaKey(Visa, otherProduct, Debit, null))
            .Should().Be(expected);
    }
}
