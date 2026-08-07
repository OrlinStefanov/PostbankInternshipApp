using BinTool.Core.Entities;
using BinTool.Core.Models.Import;
using BinTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

/// <summary>
/// The conflict lifecycle after an import: reading back what is still pending and
/// applying the user's update/discard decisions.
/// </summary>
public class BinConflictWorkflowTests : ImportTestBase
{
    /// <summary>
    /// Imports a row that collides with a seeded record, returning the staged conflict.
    /// </summary>
    private async Task<BinConflict> StageConflict(
        string prefix = "400001", string scheme = "Mastercard", string funding = "Debit")
    {
        SeedBinRange(prefix, VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));

        var result = await Run($"{prefix},{scheme},Consumer,{funding},US,2024-01-01,");
        return result.Conflicts.Single();
    }

    // ---- Reading pending conflicts --------------------------------------------

    [Fact]
    public async Task No_conflicts_returns_an_empty_list()
    {
        var conflicts = await Service.GetPendingConflictsAsync();

        conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task Pending_conflicts_are_returned_with_their_diff()
    {
        var staged = await StageConflict();

        var conflicts = await Service.GetPendingConflictsAsync();

        var conflict = conflicts.Single();
        conflict.PendingBinConflictId.Should().Be(staged.PendingBinConflictId);
        conflict.Prefix.Should().Be("400001");
        conflict.Differences.Should().Contain(d =>
            d.Field == "CardScheme" && d.OldValue == "Visa" && d.NewValue == "Mastercard");
        conflict.Differences.Should().Contain(d =>
            d.Field == "FundingType" && d.OldValue == "Credit" && d.NewValue == "Debit");
    }

    [Fact]
    public async Task Pending_conflicts_survive_a_new_service_instance()
    {
        // Simulates the user reloading the page: a fresh context and service must be
        // able to rebuild the outstanding worklist from the database alone.
        var staged = await StageConflict();

        await using var fresh = NewContext();
        var freshService = new BinCsvImportService(
            fresh, CurrentUser, new AuditLog(fresh, CurrentUser), new CardSchemeDetector());

        var conflicts = await freshService.GetPendingConflictsAsync();

        conflicts.Single().PendingBinConflictId.Should().Be(staged.PendingBinConflictId);
        conflicts.Single().Differences.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Resolved_conflicts_are_excluded()
    {
        var staged = await StageConflict();

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = true }
        });

        var conflicts = await Service.GetPendingConflictsAsync();

        conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task Discarded_conflicts_are_excluded()
    {
        var staged = await StageConflict();

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = false }
        });

        var conflicts = await Service.GetPendingConflictsAsync();

        conflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task Diff_is_recomputed_against_the_current_record()
    {
        await StageConflict(); // existing Visa/Credit vs incoming Mastercard/Debit

        // The underlying record changes after the conflict was staged.
        var range = await Db.BinRanges.SingleAsync();
        range.CardSchemeId = AmexId;
        await Db.SaveChangesAsync();

        var conflict = (await Service.GetPendingConflictsAsync()).Single();

        conflict.Differences.Should().Contain(d =>
            d.Field == "CardScheme"
            && d.OldValue == "American Express"
            && d.NewValue == "Mastercard");
    }

    [Fact]
    public async Task A_range_with_a_pending_conflict_cannot_be_deleted()
    {
        await StageConflict();

        // The conflict's foreign key is Restrict, so the row it points at is pinned for
        // as long as the decision is outstanding - a staged conflict can never be left
        // pointing at nothing.
        await using var other = NewContext();
        other.BinRanges.RemoveRange(other.BinRanges);

        var delete = async () => await other.SaveChangesAsync();

        await delete.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Conflicts_from_several_imports_are_all_returned()
    {
        await StageConflict("400001");
        await StageConflict("400002");

        var conflicts = await Service.GetPendingConflictsAsync();

        conflicts.Should().HaveCount(2);
        conflicts.Select(c => c.Prefix).Should().BeEquivalentTo(new[] { "400001", "400002" });
        Db.ImportHistories.Should().HaveCount(2);
    }

    // ---- Applying decisions ----------------------------------------------------

    [Fact]
    public async Task Update_overwrites_the_existing_row_in_place()
    {
        var staged = await StageConflict();
        var originalId = Db.BinRanges.Single().BinRangeId;

        var result = await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = true }
        });

        result.UpdatedCount.Should().Be(1);
        result.DiscardedCount.Should().Be(0);
        result.NotFoundCount.Should().Be(0);

        var row = Db.BinRanges.Single();
        row.BinRangeId.Should().Be(originalId); // same record, not a replacement
        row.CardSchemeId.Should().Be(MastercardId);
        row.FundingTypeId.Should().Be(DebitId);
        row.UpdatedBy.Should().Be("system");

        Db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Applied);
    }

    [Fact]
    public async Task Applying_a_conflict_increments_the_import_history_updated_count()
    {
        var staged = await StageConflict();

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = true }
        });

        // The update lands against the import that raised the conflict, not the import run
        // itself (which only inserts and stages) - so its UpdatedRows now reflects the one
        // existing row the file ultimately changed.
        Db.ImportHistories.Single().UpdatedRows.Should().Be(1);
    }

    [Fact]
    public async Task Discarding_a_conflict_leaves_the_import_history_updated_count_at_zero()
    {
        var staged = await StageConflict();

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = false }
        });

        Db.ImportHistories.Single().UpdatedRows.Should().Be(0);
    }

    [Fact]
    public async Task Discard_leaves_the_existing_row_untouched()
    {
        var staged = await StageConflict();

        var result = await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = false }
        });

        result.DiscardedCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);

        var row = Db.BinRanges.Single();
        row.CardSchemeId.Should().Be(VisaId);
        row.FundingTypeId.Should().Be(CreditId);

        Db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Discarded);
    }

    [Fact]
    public async Task Resolution_records_who_decided_and_when()
    {
        var staged = await StageConflict();

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = true }
        });

        var conflict = Db.PendingBinConflicts.Single();
        conflict.ResolvedBy.Should().Be("system");
        conflict.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task A_batch_can_mix_updates_and_discards()
    {
        var first = await StageConflict("400001");
        var second = await StageConflict("400002");

        var result = await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = first.PendingBinConflictId, Update = true },
            new ConflictResolution { PendingBinConflictId = second.PendingBinConflictId, Update = false }
        });

        result.UpdatedCount.Should().Be(1);
        result.DiscardedCount.Should().Be(1);

        var updated = Db.BinRanges.Single(b => b.Prefix == "400001");
        updated.CardSchemeId.Should().Be(MastercardId);

        var kept = Db.BinRanges.Single(b => b.Prefix == "400002");
        kept.CardSchemeId.Should().Be(VisaId);
    }

    [Fact]
    public async Task Unknown_conflict_id_is_counted_as_not_found()
    {
        var result = await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = 9999, Update = true }
        });

        result.NotFoundCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);
        result.DiscardedCount.Should().Be(0);
    }

    [Fact]
    public async Task Resolving_the_same_conflict_twice_reports_it_as_not_found()
    {
        var staged = await StageConflict();
        var decision = new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = true }
        };

        await Service.ResolveConflictsAsync(decision);
        var second = await Service.ResolveConflictsAsync(decision);

        second.NotFoundCount.Should().Be(1);
        second.UpdatedCount.Should().Be(0);
    }

    [Fact]
    public async Task Duplicate_decisions_for_one_conflict_apply_only_the_last()
    {
        var staged = await StageConflict();

        var result = await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = false },
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = true }
        });

        result.UpdatedCount.Should().Be(1);
        result.DiscardedCount.Should().Be(0);
        Db.BinRanges.Single().CardSchemeId.Should().Be(MastercardId);
    }

    [Fact]
    public async Task An_empty_decision_batch_changes_nothing()
    {
        await StageConflict();

        var result = await Service.ResolveConflictsAsync(Array.Empty<ConflictResolution>());

        result.UpdatedCount.Should().Be(0);
        result.DiscardedCount.Should().Be(0);
        result.NotFoundCount.Should().Be(0);
        Db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Pending);
    }

    [Fact]
    public async Task Applied_update_is_persisted_for_later_requests()
    {
        var staged = await StageConflict();

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = true }
        });

        await using var fresh = NewContext();
        var row = await fresh.BinRanges.SingleAsync();
        row.CardSchemeId.Should().Be(MastercardId);
        row.FundingTypeId.Should().Be(DebitId);
    }

    [Fact]
    public async Task Re_importing_after_an_update_reports_the_row_as_unchanged()
    {
        var staged = await StageConflict();

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = staged.PendingBinConflictId, Update = true }
        });

        // The same file that previously conflicted now matches the stored record.
        var result = await Run("400001,Mastercard,Consumer,Debit,US,2024-01-01,");

        result.UnchangedCount.Should().Be(1);
        result.ConflictCount.Should().Be(0);
    }
}
