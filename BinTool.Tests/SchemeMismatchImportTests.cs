using BinTool.Application.Models.Import;
using BinTool.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

// An import row whose declared card scheme contradicts the network its prefix belongs to (or whose
// prefix belongs to no known network) is not trusted: it is staged as a scheme-mismatch conflict
// for a human to accept or reject, rather than landing in the BIN data as declared.
public class SchemeMismatchImportTests : ImportTestBase
{
    // A Mastercard-range prefix (52…) the file wrongly labels as Visa.
    private const string MislabelledRow = "520001,Visa,Consumer,Credit,US,2024-01-01,";

    [Fact]
    public async Task A_declared_scheme_that_contradicts_the_prefix_is_staged_not_inserted()
    {
        var result = await Run(MislabelledRow);

        result.ConflictCount.Should().Be(1);
        result.InsertedCount.Should().Be(0);
        Db.BinRanges.Should().BeEmpty("the row is held for review, not trusted as declared");

        var conflict = result.Conflicts.Single();
        conflict.ConflictType.Should().Be("SchemeMismatch");
        conflict.Prefix.Should().Be("520001");
        conflict.Message.Should().Contain("Mastercard").And.Contain("Visa");

        var staged = Db.PendingBinConflicts.Single();
        staged.ConflictType.Should().Be(ConflictType.SchemeMismatch);
        staged.TargetBinRangeId.Should().BeNull("a brand-new prefix has no existing row to overwrite");
        staged.Status.Should().Be(ConflictStatus.Pending);
    }

    [Fact]
    public async Task A_prefix_in_no_known_range_is_flagged_as_unrecognized()
    {
        // 990000 belongs to no scheme the detector knows, so declaring any scheme for it
        // is treated as suspect.
        var result = await Run("990000,Visa,Consumer,Credit,US,2024-01-01,");

        result.ConflictCount.Should().Be(1);
        result.InsertedCount.Should().Be(0);
        result.Conflicts.Single().Message.Should().Contain("does not match any known");
    }

    [Fact]
    public async Task A_scheme_consistent_row_is_inserted_normally()
    {
        var result = await Run("520001,Mastercard,Consumer,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        result.ConflictCount.Should().Be(0);
        Db.BinRanges.Single().CardSchemeId.Should().Be(MastercardId);
    }

    [Fact]
    public async Task A_discover_row_is_trusted_once_the_scheme_exists_in_reference_data()
    {
        // Discover is not seeded, so an admin adds it before any file can declare it.
        var discover = new CardScheme { Name = "Discover", Description = "Discover Card" };
        Db.CardSchemes.Add(discover);
        await Db.SaveChangesAsync();

        var result = await Run("601100,Discover,Consumer,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        result.ConflictCount.Should().Be(0, "601100 is a Discover range and the file says so");
        Db.BinRanges.Single().CardSchemeId.Should().Be(discover.CardSchemeId);
    }

    [Fact]
    public async Task A_unionpay_row_is_trusted_under_the_name_the_sample_data_uses()
    {
        // samples/card_schemes.csv ships UnionPay with the description "China UnionPay",
        // so both spellings have to be accepted as the same network.
        var unionPay = new CardScheme { Name = "China UnionPay", Description = "China UnionPay" };
        Db.CardSchemes.Add(unionPay);
        await Db.SaveChangesAsync();

        var result = await Run("620000,China UnionPay,Consumer,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        result.ConflictCount.Should().Be(0, "620000 is a UnionPay range and the file says so");
        Db.BinRanges.Single().CardSchemeId.Should().Be(unionPay.CardSchemeId);
    }

    [Fact]
    public async Task A_mislabelled_unionpay_row_names_unionpay_in_the_reviewer_s_message()
    {
        // 62 is UnionPay's range; the file calls it Visa.
        var result = await Run("620000,Visa,Consumer,Credit,US,2024-01-01,");

        var conflict = result.Conflicts.Single();
        conflict.Message.Should().Contain("UnionPay").And.Contain("Visa");
        conflict.Message.Should().NotContain("does not match any known",
            "the detector did recognise the range, so saying otherwise misleads the reviewer");
    }

    [Fact]
    public async Task A_mislabelled_jcb_row_names_jcb_in_the_reviewer_s_message()
    {
        // 3528 is the bottom of the JCB range; the file calls it Visa.
        var result = await Run("352800,Visa,Consumer,Credit,US,2024-01-01,");

        var conflict = result.Conflicts.Single();
        conflict.Message.Should().Contain("JCB").And.Contain("Visa");
        conflict.Message.Should().NotContain("does not match any known",
            "the detector did recognise the range, so saying otherwise misleads the reviewer");
    }

    [Fact]
    public async Task Applying_a_new_prefix_mismatch_inserts_the_range_as_declared()
    {
        var import = await Run(MislabelledRow);
        var conflictId = import.Conflicts.Single().PendingBinConflictId;

        var outcome = await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = conflictId, Update = true }
        });

        outcome.UpdatedCount.Should().Be(1);

        var row = Db.BinRanges.Single();
        row.Prefix.Should().Be("520001");
        row.CardSchemeId.Should().Be(VisaId, "applying trusts the file's declared scheme");
        Db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Applied);
    }

    [Fact]
    public async Task Applying_a_new_prefix_mismatch_counts_against_the_import_as_an_insert()
    {
        var import = await Run(MislabelledRow);
        var conflictId = import.Conflicts.Single().PendingBinConflictId;

        // Nothing was imported at import time - the row was staged, so ImportedRows starts at 0.
        Db.ImportHistories.Single().ImportedRows.Should().Be(0);

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = conflictId, Update = true }
        });

        Db.ImportHistories.Single().ImportedRows.Should().Be(1);
    }

    [Fact]
    public async Task Applying_a_new_prefix_mismatch_records_an_import_audit_entry()
    {
        var import = await Run(MislabelledRow);
        var conflictId = import.Conflicts.Single().PendingBinConflictId;

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = conflictId, Update = true }
        });

        var range = Db.BinRanges.Single();
        var entry = Db.AuditEntries.AsNoTracking().Single();
        entry.Action.Should().Be(AuditAction.Imported);
        entry.EntityId.Should().Be(range.BinRangeId);
    }

    [Fact]
    public async Task Discarding_a_new_prefix_mismatch_inserts_nothing()
    {
        var import = await Run(MislabelledRow);
        var conflictId = import.Conflicts.Single().PendingBinConflictId;

        var outcome = await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = conflictId, Update = false }
        });

        outcome.DiscardedCount.Should().Be(1);
        Db.BinRanges.Should().BeEmpty();
        Db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Discarded);
    }

    [Fact]
    public async Task A_mismatch_over_an_existing_row_keeps_the_diff_and_updates_on_apply()
    {
        // The prefix already exists (correctly, as Mastercard); a later file relabels it Visa.
        var existing = SeedBinRange("520001", MastercardId, ConsumerId, CreditId, UsCountryId,
            new DateTime(2024, 1, 1));

        var import = await Run("520001,Visa,Consumer,Credit,US,2024-01-01,");

        var conflict = import.Conflicts.Single();
        conflict.ConflictType.Should().Be("SchemeMismatch");
        conflict.Differences.Should().Contain(d =>
            d.Field == "CardScheme" && d.OldValue == "Mastercard" && d.NewValue == "Visa");

        await Service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = conflict.PendingBinConflictId, Update = true }
        });

        var row = Db.BinRanges.Single();
        row.BinRangeId.Should().Be(existing.BinRangeId, "the existing row is updated in place");
        row.CardSchemeId.Should().Be(VisaId);
        Db.ImportHistories.Single().UpdatedRows.Should().Be(1);
    }

    [Fact]
    public async Task A_staged_mismatch_survives_a_reload()
    {
        await Run(MislabelledRow);

        await using var fresh = NewContext();
        var freshService = new Application.Services.BinCsvImportService(
            new Infrastructure.Repositories.BinImportRepository(fresh), CurrentUser,
            new Infrastructure.Services.AuditLog(fresh, CurrentUser),
            new Domain.Services.CardSchemeDetector(),
            new RecordingLogger<Application.Services.BinCsvImportService>());

        var conflict = (await freshService.GetPendingConflictsAsync()).Single();
        conflict.ConflictType.Should().Be("SchemeMismatch");
        conflict.Message.Should().Contain("Mastercard");
        conflict.Differences.Should().BeEmpty("a new-prefix mismatch has no existing row to diff");
    }
}
