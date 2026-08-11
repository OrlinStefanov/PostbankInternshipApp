using BinTool.Application.Models.Audit;
using FluentAssertions;

namespace BinTool.Tests;

// Pairing of an audit entry's before/after snapshots. Snapshots are written as literal JSON here
// rather than serialised from an entity, because what the diff has to cope with is whatever was
// stored at the time - including shapes no current entity produces.
public class AuditDiffTests
{
    private static AuditFieldChange Field(IReadOnlyList<AuditFieldChange> changes, string name) =>
        changes.Single(c => c.Field == name);

    [Fact]
    public void Compute_marks_a_differing_field_as_changed_and_keeps_both_values()
    {
        var changes = AuditDiff.Compute(
            """{"Prefix":"400010","CardScheme":"Visa"}""",
            """{"Prefix":"400010","CardScheme":"Mastercard"}""")!;

        Field(changes, "CardScheme").Kind.Should().Be(AuditChangeKind.Changed);
        Field(changes, "CardScheme").OldValue.Should().Be("Visa");
        Field(changes, "CardScheme").NewValue.Should().Be("Mastercard");
    }

    [Fact]
    public void Compute_marks_an_identical_field_as_unchanged()
    {
        var changes = AuditDiff.Compute(
            """{"Prefix":"400010","CardScheme":"Visa"}""",
            """{"Prefix":"400010","CardScheme":"Mastercard"}""")!;

        Field(changes, "Prefix").Kind.Should().Be(AuditChangeKind.Unchanged);
    }

    [Fact]
    public void Compute_treats_a_missing_before_snapshot_as_every_field_added()
    {
        // What a Created entry looks like: the writer stores no OldValues at all.
        var changes = AuditDiff.Compute(null, """{"Prefix":"400010","CardScheme":"Visa"}""")!;

        changes.Should().HaveCount(2);
        changes.Should().OnlyContain(c => c.Kind == AuditChangeKind.Added);
        changes.Should().OnlyContain(c => c.OldValue == null);
        Field(changes, "Prefix").NewValue.Should().Be("400010");
    }

    [Fact]
    public void Compute_treats_a_missing_after_snapshot_as_every_field_removed()
    {
        var changes = AuditDiff.Compute("""{"Prefix":"400010"}""", null)!;

        changes.Should().ContainSingle()
            .Which.Kind.Should().Be(AuditChangeKind.Removed);
    }

    [Fact]
    public void Compute_reports_fields_present_on_only_one_side()
    {
        var changes = AuditDiff.Compute(
            """{"Prefix":"400010","Retired":"yes"}""",
            """{"Prefix":"400010","Region":"Europe"}""")!;

        Field(changes, "Region").Kind.Should().Be(AuditChangeKind.Added);
        Field(changes, "Retired").Kind.Should().Be(AuditChangeKind.Removed);
        Field(changes, "Retired").OldValue.Should().Be("yes");
    }

    [Fact]
    public void Compute_orders_by_the_after_snapshot_then_appends_before_only_fields()
    {
        var changes = AuditDiff.Compute(
            """{"Retired":"yes","Prefix":"400010"}""",
            """{"CardScheme":"Visa","Prefix":"400010"}""")!;

        changes.Select(c => c.Field).Should().Equal("CardScheme", "Prefix", "Retired");
    }

    [Fact]
    public void Compute_renders_non_string_values_as_text()
    {
        var changes = AuditDiff.Compute(
            """{"PrefixLength":6,"IsDeleted":false,"ValidTo":null}""",
            """{"PrefixLength":8,"IsDeleted":true,"ValidTo":null}""")!;

        Field(changes, "PrefixLength").OldValue.Should().Be("6");
        Field(changes, "PrefixLength").NewValue.Should().Be("8");
        Field(changes, "IsDeleted").NewValue.Should().Be("true");

        // A JSON null reads as no value, and two of them are not a change.
        Field(changes, "ValidTo").NewValue.Should().BeNull();
        Field(changes, "ValidTo").Kind.Should().Be(AuditChangeKind.Unchanged);
    }

    [Fact]
    public void Compute_keeps_a_nested_value_as_compact_json_rather_than_expanding_it()
    {
        var changes = AuditDiff.Compute(
            """{"Tags":["a"]}""",
            """{"Tags":["a","b"]}""")!;

        var tags = Field(changes, "Tags");
        tags.Kind.Should().Be(AuditChangeKind.Changed);
        tags.NewValue.Should().Be("""["a","b"]""");
    }

    [Fact]
    public void Compute_returns_null_when_a_snapshot_is_malformed()
    {
        // The caller falls back to showing the snapshot as stored - a viewer that hides
        // broken data is worse than one that shows it.
        AuditDiff.Compute("not json at all", """{"Prefix":"400010"}""").Should().BeNull();
    }

    [Fact]
    public void Compute_returns_null_when_a_snapshot_is_not_an_object()
    {
        AuditDiff.Compute("""[1,2,3]""", """{"Prefix":"400010"}""").Should().BeNull();
    }

    [Fact]
    public void Compute_returns_no_fields_when_neither_snapshot_was_recorded()
    {
        AuditDiff.Compute(null, null).Should().BeEmpty();
    }
}
