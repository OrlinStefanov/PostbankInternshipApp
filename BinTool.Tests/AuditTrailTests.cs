using System.Text.Json;
using BinTool.Application.Abstractions;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Services;
using BinTool.Domain.Entities;
using BinTool.Infrastructure.Repositories;
using BinTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

// The audit trail: that every change to live BIN data leaves an entry, that the entry says what the
// values were as well as what they became, and that nothing which changed no data leaves one
// behind.
public class AuditTrailTests : ImportTestBase
{
    private const string AdminUserId = "admin-user-id";
    private static readonly DateTime Started = new(2024, 1, 1);

    private readonly BinRangeAdminService _admin;

    public AuditTrailTests()
    {
        SeedUser(AdminUserId, "admin");

        CurrentUser = new TestUser(AdminUserId, "admin");
        _admin = new BinRangeAdminService(
            new BinRangeRepository(Db), CurrentUser, new AuditLog(Db, CurrentUser),
            new CardSchemeDetector(), new RecordingLogger<BinRangeAdminService>());
    }

    private static BinRangeInput Input(
        string prefix = "400001", string cardScheme = "Visa", string productType = "Consumer",
        string fundingType = "Credit", string countryCode = "US", DateTime? validTo = null,
        bool acknowledgeSchemeMismatch = false) => new()
        {
            Prefix = prefix,
            CardScheme = cardScheme,
            ProductType = productType,
            FundingType = fundingType,
            CountryCode = countryCode,
            ValidFrom = Started,
            ValidTo = validTo,
            AcknowledgeSchemeMismatch = acknowledgeSchemeMismatch
        };

    // Reads the trail back through a fresh context, so a test only sees what was actually committed
    // rather than what is still sitting in the change tracker.
    private List<AuditEntry> Entries() =>
        NewContext().AuditEntries.AsNoTracking().OrderBy(e => e.AuditEntryId).ToList();

    private static BinRangeSnapshot? Read(string? json) =>
        json is null ? null : JsonSerializer.Deserialize<BinRangeSnapshot>(
            json, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    // ---- Hand-made changes ------------------------------------------------------

    [Fact]
    public async Task Adding_a_range_records_what_it_became_and_who_did_it()
    {
        var result = await _admin.CreateAsync(Input(validTo: new DateTime(2030, 1, 1)));

        var entry = Entries().Should().ContainSingle().Subject;
        entry.Action.Should().Be(AuditAction.Created);
        entry.EntityType.Should().Be(AuditEntityTypes.BinRange);
        entry.EntityId.Should().Be(result.Range!.BinRangeId);
        entry.PerformedByUserId.Should().Be(AdminUserId);
        entry.PerformedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        entry.OldValues.Should().BeNull("there was nothing there before");

        var after = Read(entry.NewValues)!;
        after.Prefix.Should().Be("400001");
        after.CardScheme.Should().Be("Visa");
        after.ValidFrom.Should().Be("2024-01-01");
        after.ValidTo.Should().Be("2030-01-01");
        after.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Editing_a_range_records_both_sides_of_the_change()
    {
        var created = await _admin.CreateAsync(Input());

        await _admin.UpdateAsync(created.Range!.BinRangeId, Input(
            cardScheme: "Mastercard", fundingType: "Debit", countryCode: "BG",
            acknowledgeSchemeMismatch: true));

        var entry = Entries().Last();
        entry.Action.Should().Be(AuditAction.Updated);

        var before = Read(entry.OldValues)!;
        var after = Read(entry.NewValues)!;

        before.CardScheme.Should().Be("Visa");
        before.FundingType.Should().Be("Credit");
        before.CountryCode.Should().Be("US");

        after.CardScheme.Should().Be("Mastercard");
        after.FundingType.Should().Be("Debit");
        after.CountryCode.Should().Be("BG");
    }

    [Fact]
    public async Task Reference_data_is_recorded_by_name_in_the_caller_s_own_spelling_or_the_stored_one()
    {
        // "visa" resolves to Visa. The entry has to show the canonical name on both sides,
        // or a case difference would read as a change that never happened.
        var created = await _admin.CreateAsync(Input());
        await _admin.UpdateAsync(created.Range!.BinRangeId, Input(cardScheme: "visa"));

        var entry = Entries().Last();

        Read(entry.OldValues)!.CardScheme.Should().Be("Visa");
        Read(entry.NewValues)!.CardScheme.Should().Be("Visa");
    }

    [Fact]
    public async Task Deleting_and_restoring_a_range_are_both_recorded()
    {
        var created = await _admin.CreateAsync(Input());
        var id = created.Range!.BinRangeId;

        await _admin.DeleteAsync(id);
        await _admin.RestoreAsync(id);

        var entries = Entries();
        entries.Should().HaveCount(3);

        var deleted = entries[1];
        deleted.Action.Should().Be(AuditAction.Deleted);
        Read(deleted.OldValues)!.IsDeleted.Should().BeFalse();
        Read(deleted.NewValues)!.IsDeleted.Should().BeTrue();

        var restored = entries[2];
        restored.Action.Should().Be(AuditAction.Updated);
        Read(restored.OldValues)!.IsDeleted.Should().BeTrue();
        Read(restored.NewValues)!.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task A_refused_change_leaves_no_entry()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        await _admin.CreateAsync(Input());                              // prefix in use
        await _admin.CreateAsync(Input(prefix: "40", cardScheme: "x")); // invalid
        await _admin.UpdateAsync(999, Input(prefix: "400002"));         // not found
        await _admin.DeleteAsync(999);                                  // not found

        Entries().Should().BeEmpty("nothing changed, so there is nothing to account for");
    }

    // ---- Imported changes -------------------------------------------------------

    [Fact]
    public async Task An_imported_range_is_recorded_as_imported_rather_than_created()
    {
        // The trail should distinguish a range that arrived in a file from one typed in.
        await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        var entry = Entries().Should().ContainSingle().Subject;
        entry.Action.Should().Be(AuditAction.Imported);
        entry.EntityType.Should().Be(AuditEntityTypes.BinRange);
        entry.PerformedByUserId.Should().Be(AdminUserId);

        entry.OldValues.Should().BeNull();
        Read(entry.NewValues)!.Prefix.Should().Be("400001");
    }

    [Fact]
    public async Task Each_imported_range_gets_its_own_entry_pointing_at_its_own_row()
    {
        await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",
            "400002,Visa,Consumer,Debit,BG,2024-01-01,");

        var entries = Entries();
        entries.Should().HaveCount(2);

        var ranges = await NewContext().BinRanges.AsNoTracking()
            .ToDictionaryAsync(b => b.BinRangeId, b => b.Prefix);

        entries.Select(e => ranges[e.EntityId]).Should().BeEquivalentTo("400001", "400002");
    }

    [Fact]
    public async Task Reviving_a_deleted_range_by_import_records_what_it_replaced()
    {
        // A Mastercard-range prefix (52) declared as Mastercard, so the import revives the
        // soft-deleted row rather than staging a scheme mismatch.
        var deleted = SeedBinRange("520001", VisaId, ConsumerId, CreditId, UsCountryId, Started);
        deleted.IsDeleted = true;
        Db.SaveChanges();

        await Run("520001,Mastercard,Commercial,Debit,BG,2024-06-01,");

        var entry = Entries().Should().ContainSingle().Subject;
        entry.EntityId.Should().Be(deleted.BinRangeId);

        var before = Read(entry.OldValues)!;
        before.CardScheme.Should().Be("Visa");
        before.IsDeleted.Should().BeTrue();

        var after = Read(entry.NewValues)!;
        after.CardScheme.Should().Be("Mastercard");
        after.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Rows_that_changed_nothing_leave_no_entry()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var result = await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,", // unchanged
            "40,Visa,Consumer,Credit,US,2024-01-01,",     // rejected
            "400002,Visa,Consumer,Credit,ZZ,2024-01-01,"); // rejected: unknown country

        result.UnchangedCount.Should().Be(1);
        result.RejectedCount.Should().Be(2);

        Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task A_staged_conflict_is_not_audited_until_someone_applies_it()
    {
        var existing = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var import = await Run("400001,Mastercard,Consumer,Credit,US,2024-01-01,");
        import.ConflictCount.Should().Be(1);

        Entries().Should().BeEmpty("nothing has been decided, so no BIN data has changed");

        await Service.ResolveConflictsAsync(new[]
        {
            new Application.Models.Import.ConflictResolution
            {
                PendingBinConflictId = import.Conflicts[0].PendingBinConflictId,
                Update = true
            }
        });

        var entry = Entries().Should().ContainSingle().Subject;
        entry.Action.Should().Be(AuditAction.Updated);
        entry.EntityId.Should().Be(existing.BinRangeId);
        Read(entry.OldValues)!.CardScheme.Should().Be("Visa");
        Read(entry.NewValues)!.CardScheme.Should().Be("Mastercard");
    }

    [Fact]
    public async Task Discarding_a_conflict_leaves_no_entry()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, Started);

        var import = await Run("400001,Mastercard,Consumer,Credit,US,2024-01-01,");

        await Service.ResolveConflictsAsync(new[]
        {
            new Application.Models.Import.ConflictResolution
            {
                PendingBinConflictId = import.Conflicts[0].PendingBinConflictId,
                Update = false
            }
        });

        Entries().Should().BeEmpty("the stored range was kept, so no BIN data changed");
    }

    [Fact]
    public async Task An_import_with_no_signed_in_user_still_records_the_change()
    {
        // The user reference is nullable precisely so the trail survives this rather than
        // losing the entry.
        CurrentUser = new SystemCurrentUser();

        await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        var entry = Entries().Should().ContainSingle().Subject;
        entry.PerformedByUserId.Should().BeNull();
        entry.Action.Should().Be(AuditAction.Imported);
    }

    private sealed class TestUser : ICurrentUser
    {
        public TestUser(string userId, string name)
        {
            UserId = userId;
            Name = name;
        }

        public string? UserId { get; }

        public string Name { get; }
    }
}
