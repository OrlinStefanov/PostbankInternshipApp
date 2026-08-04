using System.Text;
using BinTool.Core.Entities;
using BinTool.Core.Models.Import;
using BinTool.Infrastructure.Data;
using BinTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

public class BinCsvImportServiceTests
{
    private const string Header =
        "Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom,ValidTo";

    // Ids match the seeded reference data applied by EnsureCreated.
    private const int VisaId = 1, MastercardId = 2;
    private const int ConsumerId = 1;
    private const int CreditId = 1, DebitId = 2;
    private const int UsCountryId = 11;

    private readonly AppDbContext _db;
    private readonly BinCsvImportService _service;

    public BinCsvImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated(); // seeds the lookup tables
        _service = new BinCsvImportService(_db);
    }

    /// <summary>
    /// Builds an in-memory CSV from the header plus the given data rows and imports it.
    /// </summary>
    private Task<BinImportResult> Run(params string[] dataRows) => RunWithHeader(Header, dataRows);

    private Task<BinImportResult> RunWithHeader(string header, params string[] dataRows)
    {
        var content = new StringBuilder().AppendLine(header);
        foreach (var row in dataRows)
            content.AppendLine(row);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content.ToString()));
        return _service.ImportAsync(stream, "test.csv");
    }

    /// <summary>
    /// Seeds an existing BIN range so reconciliation has something to compare against.
    /// </summary>
    private void SeedBinRange(string prefix, int cardSchemeId, int productTypeId,
        int fundingTypeId, int countryId, DateTime validFrom, DateTime? validTo = null)
    {
        _db.BinRanges.Add(new BinRange
        {
            Prefix = prefix,
            PrefixLength = prefix.Length,
            CardSchemeId = cardSchemeId,
            ProductTypeId = productTypeId,
            FundingTypeId = fundingTypeId,
            CountryId = countryId,
            ValidFrom = validFrom,
            ValidTo = validTo
        });
        _db.SaveChanges();
    }

    // ---- Insert (new prefix) --------------------------------------------------

    [Fact]
    public async Task New_prefix_is_inserted()
    {
        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,2026-12-31");

        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);
        result.ConflictCount.Should().Be(0);

        var row = _db.BinRanges.Single();
        row.Prefix.Should().Be("400001");
        row.CardSchemeId.Should().Be(VisaId);
        row.ProductTypeId.Should().Be(ConsumerId);
        row.FundingTypeId.Should().Be(CreditId);
        row.CountryId.Should().Be(UsCountryId);
        row.ValidFrom.Should().Be(new DateTime(2024, 1, 1));
        row.ValidTo.Should().Be(new DateTime(2026, 12, 31));
        row.CreatedBy.Should().Be("system");
    }

    [Fact]
    public async Task Blank_ValidTo_is_inserted_as_open_ended()
    {
        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        _db.BinRanges.Single().ValidTo.Should().BeNull();
    }

    [Theory]
    [InlineData("123456")]   // 6 digits - lower boundary
    [InlineData("12345678")] // 8 digits - upper boundary
    public async Task Prefix_at_length_boundaries_is_accepted(string prefix)
    {
        var result = await Run($"{prefix},Visa,Consumer,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);
    }

    // ---- Rejections (structure + unknown lookups) -----------------------------

    [Theory]
    [InlineData("12345")]      // too short
    [InlineData("123456789")]  // too long
    [InlineData("12A456")]     // non-numeric
    [InlineData("")]           // empty
    public async Task Invalid_prefix_is_rejected(string prefix)
    {
        var result = await Run($"{prefix},Visa,Consumer,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(0);
        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("Prefix must be 6-8 digits");
    }

    [Theory]
    [InlineData("400001,,Consumer,Credit,US,2024-01-01,", "CardScheme is required")]
    [InlineData("400001,Visa,,Credit,US,2024-01-01,", "ProductType is required")]
    [InlineData("400001,Visa,Consumer,,US,2024-01-01,", "FundingType is required")]
    public async Task Missing_required_lookup_field_is_rejected(string row, string expectedReason)
    {
        var result = await Run(row);

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be(expectedReason);
    }

    [Theory]
    [InlineData("USA")] // 3 letters
    [InlineData("U")]   // 1 letter
    [InlineData("U1")]  // contains a digit
    public async Task Invalid_country_code_is_rejected(string code)
    {
        var result = await Run($"400001,Visa,Consumer,Credit,{code},2024-01-01,");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("CountryCode must be a 2-letter ISO code");
    }

    [Theory]
    [InlineData("400001,Discover,Consumer,Credit,US,2024-01-01,", "CardScheme 'Discover' does not exist")]
    [InlineData("400001,Visa,Platinum,Credit,US,2024-01-01,", "ProductType 'Platinum' does not exist")]
    [InlineData("400001,Visa,Consumer,Charge,US,2024-01-01,", "FundingType 'Charge' does not exist")]
    [InlineData("400001,Visa,Consumer,Credit,ZZ,2024-01-01,", "CountryCode 'ZZ' does not exist")]
    public async Task Unknown_lookup_value_is_rejected(string row, string expectedReason)
    {
        var result = await Run(row);

        result.InsertedCount.Should().Be(0);
        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be(expectedReason);
    }

    [Fact]
    public async Task Malformed_ValidFrom_is_rejected_without_failing_the_batch()
    {
        var result = await Run(
            "400001,Visa,Consumer,Credit,US,2024-13-99,",   // invalid month/day
            "400002,Visa,Consumer,Debit,BG,2024-01-01,");   // valid

        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("ValidFrom must be a valid date (yyyy-MM-dd)");
        result.Errors.Single().RowNumber.Should().Be(1);
    }

    [Fact]
    public async Task ValidTo_before_ValidFrom_is_rejected()
    {
        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,2023-01-01");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("ValidTo must be after ValidFrom");
    }

    [Fact]
    public async Task Duplicate_prefix_within_file_rejects_the_later_row()
    {
        var result = await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",
            "400001,Mastercard,Commercial,Debit,BG,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Contain("Duplicate prefix '400001'");
        result.Errors.Single().RowNumber.Should().Be(2);
    }

    [Fact]
    public async Task Missing_required_column_returns_a_single_header_error()
    {
        var result = await RunWithHeader(
            "Prefix,CardScheme,FundingType,CountryCode,ValidFrom,ValidTo", // no ProductType
            "400001,Visa,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(0);
        result.TotalRows.Should().Be(0); // bails out before reading data rows
        result.Errors.Single().Reason.Should().Contain("Missing required column(s)");
        result.Errors.Single().Reason.Should().Contain("ProductType");
    }

    [Fact]
    public async Task Empty_file_is_reported_as_error()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

        var result = await _service.ImportAsync(stream, "empty.csv");

        result.InsertedCount.Should().Be(0);
        result.Errors.Single().Reason.Should().Be("File is empty or has no header row");
    }

    // ---- Reconciliation (unchanged / conflict) --------------------------------

    [Fact]
    public async Task Existing_identical_row_is_skipped_as_unchanged()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));

        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        result.UnchangedCount.Should().Be(1);
        result.InsertedCount.Should().Be(0);
        result.ConflictCount.Should().Be(0);
        _db.PendingBinConflicts.Should().BeEmpty();
        _db.BinRanges.Should().HaveCount(1); // nothing added
    }

    [Fact]
    public async Task Existing_row_with_different_values_is_staged_as_conflict()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));

        // Same prefix, different card scheme + funding type.
        var result = await Run("400001,Mastercard,Consumer,Debit,US,2024-01-01,");

        result.ConflictCount.Should().Be(1);
        result.InsertedCount.Should().Be(0);
        result.UnchangedCount.Should().Be(0);

        var conflict = result.Conflicts.Single();
        conflict.Prefix.Should().Be("400001");
        conflict.PendingBinConflictId.Should().BeGreaterThan(0);
        conflict.Differences.Should().Contain(d =>
            d.Field == "CardScheme" && d.OldValue == "Visa" && d.NewValue == "Mastercard");
        conflict.Differences.Should().Contain(d =>
            d.Field == "FundingType" && d.OldValue == "Credit" && d.NewValue == "Debit");
        conflict.Differences.Should().HaveCount(2);

        // The existing row is untouched until the user decides.
        _db.BinRanges.Single().CardSchemeId.Should().Be(VisaId);
        _db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Pending);
    }

    // ---- Conflict resolution --------------------------------------------------

    [Fact]
    public async Task Resolving_a_conflict_with_update_overwrites_the_existing_row()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));
        var import = await Run("400001,Mastercard,Consumer,Debit,US,2024-01-01,");
        var conflictId = import.Conflicts.Single().PendingBinConflictId;

        var result = await _service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = conflictId, Update = true }
        });

        result.UpdatedCount.Should().Be(1);
        result.DiscardedCount.Should().Be(0);

        var row = _db.BinRanges.Single();
        row.CardSchemeId.Should().Be(MastercardId);
        row.FundingTypeId.Should().Be(DebitId);
        row.UpdatedBy.Should().Be("system");
        _db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Applied);
    }

    [Fact]
    public async Task Resolving_a_conflict_with_discard_leaves_the_existing_row()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));
        var import = await Run("400001,Mastercard,Consumer,Debit,US,2024-01-01,");
        var conflictId = import.Conflicts.Single().PendingBinConflictId;

        var result = await _service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = conflictId, Update = false }
        });

        result.DiscardedCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);

        _db.BinRanges.Single().CardSchemeId.Should().Be(VisaId); // unchanged
        _db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Discarded);
    }

    [Fact]
    public async Task Resolving_an_unknown_conflict_id_is_counted_as_not_found()
    {
        var result = await _service.ResolveConflictsAsync(new[]
        {
            new ConflictResolution { PendingBinConflictId = 9999, Update = true }
        });

        result.NotFoundCount.Should().Be(1);
        result.UpdatedCount.Should().Be(0);
        result.DiscardedCount.Should().Be(0);
    }

    // ---- Persistence side effects ---------------------------------------------

    [Fact]
    public async Task Rejected_rows_are_persisted_with_the_import_history()
    {
        var result = await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",  // inserted
            "12345,Visa,Consumer,Credit,US,2024-01-01,");  // invalid prefix

        var history = _db.ImportHistories.Single();
        history.ImportHistoryId.Should().Be(result.ImportHistoryId);
        history.ImportedRows.Should().Be(1);
        history.RejectedRows.Should().Be(1);
        history.Status.Should().Be("Partial");

        var rejected = _db.RejectedImportRows.Single();
        rejected.ImportHistoryId.Should().Be(history.ImportHistoryId);
        rejected.RowNumber.Should().Be(2);
        rejected.Reason.Should().Be("Prefix must be 6-8 digits");
    }

    [Fact]
    public async Task Counts_are_consistent_across_all_buckets()
    {
        SeedBinRange("400003", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));

        var result = await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",       // inserted
            "400003,Mastercard,Consumer,Credit,US,2024-01-01,", // conflict
            "12345,Visa,Consumer,Credit,US,2024-01-01,");       // rejected

        result.FileName.Should().Be("test.csv");
        result.TotalRows.Should().Be(3);
        result.ConflictCount.Should().Be(result.Conflicts.Count);
        result.RejectedCount.Should().Be(result.Errors.Count);
        (result.InsertedCount + result.UnchangedCount + result.ConflictCount + result.RejectedCount)
            .Should().Be(result.TotalRows);
    }
}
