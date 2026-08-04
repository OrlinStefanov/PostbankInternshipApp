using System.Text;
using BinTool.Core.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

/// <summary>
/// Parsing, validation and reconciliation behaviour of a single import run.
/// </summary>
public class BinCsvImportServiceTests : ImportTestBase
{
    // ---- Insert (new prefix) --------------------------------------------------

    [Fact]
    public async Task New_prefix_is_inserted()
    {
        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,2026-12-31");

        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);
        result.ConflictCount.Should().Be(0);

        var row = Db.BinRanges.Single();
        row.Prefix.Should().Be("400001");
        row.PrefixLength.Should().Be(6);
        row.CardSchemeId.Should().Be(VisaId);
        row.ProductTypeId.Should().Be(ConsumerId);
        row.FundingTypeId.Should().Be(CreditId);
        row.CountryId.Should().Be(UsCountryId);
        row.ValidFrom.Should().Be(new DateTime(2024, 1, 1));
        row.ValidTo.Should().Be(new DateTime(2026, 12, 31));
        row.CreatedBy.Should().Be("system");
        row.UpdatedBy.Should().Be("system");
    }

    [Fact]
    public async Task Blank_ValidTo_is_inserted_as_open_ended()
    {
        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        Db.BinRanges.Single().ValidTo.Should().BeNull();
    }

    [Fact]
    public async Task File_without_the_optional_ValidTo_column_is_accepted()
    {
        var result = await RunWithHeader(
            "Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom",
            "400001,Visa,Consumer,Credit,US,2024-01-01");

        result.InsertedCount.Should().Be(1);
        Db.BinRanges.Single().ValidTo.Should().BeNull();
    }

    [Theory]
    [InlineData("123456")]   // 6 digits - lower boundary
    [InlineData("12345678")] // 8 digits - upper boundary
    public async Task Prefix_at_length_boundaries_is_accepted(string prefix)
    {
        var result = await Run($"{prefix},Visa,Consumer,Credit,US,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);
        Db.BinRanges.Single().PrefixLength.Should().Be(prefix.Length);
    }

    [Fact]
    public async Task Multiple_new_rows_are_all_inserted()
    {
        var result = await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",
            "400002,Mastercard,Commercial,Debit,BG,2024-02-01,",
            "400003,American Express,Prepaid,Credit,DE,2024-03-01,");

        result.InsertedCount.Should().Be(3);
        result.RejectedCount.Should().Be(0);
        Db.BinRanges.Should().HaveCount(3);
    }

    // ---- Parsing robustness ---------------------------------------------------

    [Fact]
    public async Task Surrounding_whitespace_is_trimmed_from_every_field()
    {
        var result = await Run("  400001 , Visa , Consumer , Credit , US , 2024-01-01 , ");

        result.InsertedCount.Should().Be(1);

        var row = Db.BinRanges.Single();
        row.Prefix.Should().Be("400001");
        row.CardSchemeId.Should().Be(VisaId);
        row.ValidTo.Should().BeNull();
    }

    [Fact]
    public async Task Lookup_names_are_matched_case_insensitively()
    {
        var result = await Run("400001,visa,CONSUMER,cReDiT,us,2024-01-01,");

        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);

        var row = Db.BinRanges.Single();
        row.CardSchemeId.Should().Be(VisaId);
        row.ProductTypeId.Should().Be(ConsumerId);
        row.FundingTypeId.Should().Be(CreditId);
        row.CountryId.Should().Be(UsCountryId);
    }

    [Fact]
    public async Task Quoted_fields_are_parsed()
    {
        var result = await Run("\"400001\",\"Visa\",\"Consumer\",\"Credit\",\"US\",\"2024-01-01\",\"\"");

        result.InsertedCount.Should().Be(1);
        Db.BinRanges.Single().Prefix.Should().Be("400001");
    }

    [Fact]
    public async Task Crlf_line_endings_are_handled()
    {
        var csv = Header + "\r\n"
                + "400001,Visa,Consumer,Credit,US,2024-01-01,\r\n"
                + "400002,Visa,Consumer,Debit,BG,2024-01-01,\r\n";

        var result = await RunRaw(csv);

        result.TotalRows.Should().Be(2);
        result.InsertedCount.Should().Be(2);
    }

    [Fact]
    public async Task Utf8_byte_order_mark_does_not_break_the_header()
    {
        // Excel writes CSVs with a BOM; without stripping it the first column name
        // would be "﻿Prefix" and the header check would reject the file.
        var csv = "﻿" + Header + "\n400001,Visa,Consumer,Credit,US,2024-01-01,\n";

        var result = await RunBytes(Encoding.UTF8.GetBytes(csv));

        result.RejectedCount.Should().Be(0);
        result.InsertedCount.Should().Be(1);
    }

    [Fact]
    public async Task Row_with_too_few_columns_is_rejected_without_aborting_the_import()
    {
        var result = await Run(
            "400001,Visa,Consumer",                        // truncated row
            "400002,Visa,Consumer,Debit,BG,2024-01-01,");  // valid

        result.TotalRows.Should().Be(2);
        result.InsertedCount.Should().Be(1);
        result.RejectedCount.Should().Be(1);
        result.Errors.Single().RowNumber.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("FundingType is required");
    }

    [Fact]
    public async Task Rejected_row_keeps_its_raw_content_for_inspection()
    {
        var result = await Run("12345,Visa,Consumer,Credit,US,2024-01-01,");

        result.Errors.Single().RawData.Should().Contain("12345,Visa,Consumer");
    }

    // ---- Rejections (structure) -----------------------------------------------

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
    [InlineData("")]    // empty
    public async Task Invalid_country_code_is_rejected(string code)
    {
        var result = await Run($"400001,Visa,Consumer,Credit,{code},2024-01-01,");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("CountryCode must be a 2-letter ISO code");
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

    [Theory]
    [InlineData("01/01/2024")] // wrong format
    [InlineData("2024-1-1")]   // not zero-padded
    [InlineData("not-a-date")]
    public async Task ValidFrom_in_the_wrong_format_is_rejected(string date)
    {
        var result = await Run($"400001,Visa,Consumer,Credit,US,{date},");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("ValidFrom must be a valid date (yyyy-MM-dd)");
    }

    [Fact]
    public async Task Malformed_ValidTo_is_rejected()
    {
        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,2026-99-99");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("ValidTo must be a valid date (yyyy-MM-dd)");
    }

    [Fact]
    public async Task ValidTo_before_ValidFrom_is_rejected()
    {
        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,2023-01-01");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("ValidTo must be after ValidFrom");
    }

    [Fact]
    public async Task ValidTo_equal_to_ValidFrom_is_rejected()
    {
        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,2024-01-01");

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

    // ---- Rejections (unknown lookup values) -----------------------------------

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
    public async Task Soft_deleted_lookup_value_is_treated_as_unknown()
    {
        var visa = Db.CardSchemes.Single(c => c.CardSchemeId == VisaId);
        visa.IsDeleted = true;
        await Db.SaveChangesAsync();

        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("CardScheme 'Visa' does not exist");
    }

    // ---- Header / file level ---------------------------------------------------

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
    public async Task All_missing_columns_are_listed_in_one_error()
    {
        var result = await RunWithHeader("Prefix,CardScheme", "400001,Visa");

        var reason = result.Errors.Single().Reason;
        reason.Should().Contain("ProductType");
        reason.Should().Contain("FundingType");
        reason.Should().Contain("CountryCode");
        reason.Should().Contain("ValidFrom");
    }

    [Fact]
    public async Task Empty_file_is_reported_as_error()
    {
        var result = await RunRaw(string.Empty, "empty.csv");

        result.InsertedCount.Should().Be(0);
        result.Errors.Single().Reason.Should().Be("File is empty or has no header row");
    }

    [Fact]
    public async Task Header_only_file_imports_nothing_and_reports_no_error()
    {
        var result = await Run();

        result.TotalRows.Should().Be(0);
        result.InsertedCount.Should().Be(0);
        result.RejectedCount.Should().Be(0);
        result.ConflictCount.Should().Be(0);
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
        Db.PendingBinConflicts.Should().BeEmpty();
        Db.BinRanges.Should().HaveCount(1); // nothing added
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
        conflict.RowNumber.Should().Be(1);
        conflict.PendingBinConflictId.Should().BeGreaterThan(0);
        conflict.Differences.Should().Contain(d =>
            d.Field == "CardScheme" && d.OldValue == "Visa" && d.NewValue == "Mastercard");
        conflict.Differences.Should().Contain(d =>
            d.Field == "FundingType" && d.OldValue == "Credit" && d.NewValue == "Debit");
        conflict.Differences.Should().HaveCount(2);

        // The existing row is untouched until the user decides.
        Db.BinRanges.Single().CardSchemeId.Should().Be(VisaId);
        Db.PendingBinConflicts.Single().Status.Should().Be(ConflictStatus.Pending);
    }

    [Fact]
    public async Task Conflict_on_dates_only_is_reported_per_field()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId,
            new DateTime(2024, 1, 1), new DateTime(2026, 1, 1));

        var result = await Run("400001,Visa,Consumer,Credit,US,2024-06-01,2027-01-01");

        var diffs = result.Conflicts.Single().Differences;
        diffs.Should().HaveCount(2);
        diffs.Should().Contain(d =>
            d.Field == "ValidFrom" && d.OldValue == "2024-01-01" && d.NewValue == "2024-06-01");
        diffs.Should().Contain(d =>
            d.Field == "ValidTo" && d.OldValue == "2026-01-01" && d.NewValue == "2027-01-01");
    }

    [Fact]
    public async Task Clearing_ValidTo_is_reported_as_a_conflict_with_a_null_new_value()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId,
            new DateTime(2024, 1, 1), new DateTime(2026, 1, 1));

        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        var diff = result.Conflicts.Single().Differences.Single();
        diff.Field.Should().Be("ValidTo");
        diff.OldValue.Should().Be("2026-01-01");
        diff.NewValue.Should().BeNull();
    }

    [Fact]
    public async Task A_single_file_can_produce_every_bucket_at_once()
    {
        SeedBinRange("400003", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));
        SeedBinRange("400004", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));

        var result = await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",       // inserted
            "400003,Mastercard,Consumer,Credit,US,2024-01-01,", // conflict
            "400004,Visa,Consumer,Credit,US,2024-01-01,",       // unchanged
            "12345,Visa,Consumer,Credit,US,2024-01-01,");       // rejected

        result.TotalRows.Should().Be(4);
        result.InsertedCount.Should().Be(1);
        result.ConflictCount.Should().Be(1);
        result.UnchangedCount.Should().Be(1);
        result.RejectedCount.Should().Be(1);
    }

    // ---- Soft-deleted ranges ---------------------------------------------------

    [Fact]
    public async Task Importing_a_soft_deleted_prefix_revives_the_existing_row()
    {
        // The unique index on Prefix covers soft-deleted rows, so inserting a second
        // row for the same prefix would breach it. The range is revived instead.
        var deleted = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId,
            new DateTime(2024, 1, 1));
        deleted.IsDeleted = true;
        deleted.DeletedAt = new DateTime(2025, 1, 1);
        deleted.DeletedBy = "someone";
        await Db.SaveChangesAsync();

        var result = await Run("400001,Mastercard,Commercial,Debit,BG,2024-06-01,");

        result.InsertedCount.Should().Be(1);
        result.ConflictCount.Should().Be(0);
        result.RejectedCount.Should().Be(0);

        await using var fresh = NewContext();
        var row = await fresh.BinRanges.SingleAsync(); // still exactly one row
        row.BinRangeId.Should().Be(deleted.BinRangeId); // same record, revived in place
        row.IsDeleted.Should().BeFalse();
        row.DeletedAt.Should().BeNull();
        row.DeletedBy.Should().BeNull();
        row.CardSchemeId.Should().Be(MastercardId);
        row.ProductTypeId.Should().Be(CommercialId);
        row.FundingTypeId.Should().Be(DebitId);
        row.CountryId.Should().Be(BgCountryId);
        row.ValidFrom.Should().Be(new DateTime(2024, 6, 1));
        row.UpdatedBy.Should().Be("system");
    }

    [Fact]
    public async Task Reviving_a_soft_deleted_prefix_does_not_stage_a_conflict()
    {
        var deleted = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId,
            new DateTime(2024, 1, 1));
        deleted.IsDeleted = true;
        await Db.SaveChangesAsync();

        // Different values from the deleted row: there is no live record to arbitrate,
        // so this must not ask the user to decide anything.
        var result = await Run("400001,Mastercard,Consumer,Debit,US,2024-01-01,");

        result.ConflictCount.Should().Be(0);
        Db.PendingBinConflicts.Should().BeEmpty();
    }

    [Fact]
    public async Task A_revived_row_behaves_normally_on_the_next_import()
    {
        var deleted = SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId,
            new DateTime(2024, 1, 1));
        deleted.IsDeleted = true;
        await Db.SaveChangesAsync();

        await Run("400001,Mastercard,Consumer,Debit,US,2024-01-01,");

        // Same file again - now it is a live row, so it reconciles as unchanged.
        var second = await Run("400001,Mastercard,Consumer,Debit,US,2024-01-01,");
        second.UnchangedCount.Should().Be(1);
        second.InsertedCount.Should().Be(0);

        // And a differing file now goes through the conflict workflow.
        var third = await Run("400001,Visa,Consumer,Debit,US,2024-01-01,");
        third.ConflictCount.Should().Be(1);
    }

    // ---- Persistence side effects ---------------------------------------------

    [Fact]
    public async Task Rejected_rows_are_persisted_with_the_import_history()
    {
        var result = await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",  // inserted
            "12345,Visa,Consumer,Credit,US,2024-01-01,");  // invalid prefix

        var history = Db.ImportHistories.Single();
        history.ImportHistoryId.Should().Be(result.ImportHistoryId);
        history.FileName.Should().Be("test.csv");
        history.ImportedRows.Should().Be(1);
        history.RejectedRows.Should().Be(1);
        history.Status.Should().Be("Partial");

        var rejected = Db.RejectedImportRows.Single();
        rejected.ImportHistoryId.Should().Be(history.ImportHistoryId);
        rejected.RowNumber.Should().Be(2);
        rejected.Reason.Should().Be("Prefix must be 6-8 digits");
    }

    [Fact]
    public async Task Import_history_is_marked_Success_when_every_row_lands_cleanly()
    {
        await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        Db.ImportHistories.Single().Status.Should().Be("Success");
    }

    [Fact]
    public async Task Import_history_is_marked_Failed_when_every_row_is_rejected()
    {
        await Run(
            "12345,Visa,Consumer,Credit,US,2024-01-01,",
            "999,Visa,Consumer,Credit,US,2024-01-01,");

        Db.ImportHistories.Single().Status.Should().Be("Failed");
    }

    [Fact]
    public async Task Header_error_is_persisted_and_marks_the_import_Failed()
    {
        await RunWithHeader("Prefix,CardScheme", "400001,Visa");

        var history = Db.ImportHistories.Single();
        history.Status.Should().Be("Failed");
        history.RejectedRows.Should().Be(1);
        Db.RejectedImportRows.Single().RowNumber.Should().Be(0);
    }

    [Fact]
    public async Task Inserted_rows_are_visible_through_a_separate_context()
    {
        await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        await using var fresh = NewContext();
        var row = await fresh.BinRanges.SingleAsync();
        row.Prefix.Should().Be("400001");
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

    // ---- Batched prefix lookup -------------------------------------------------

    [Fact]
    public async Task Import_larger_than_the_lookup_batch_size_reconciles_every_row()
    {
        // More rows than the 500-row prefix lookup batch, so the existing-row query
        // runs over several batches and the results have to be merged correctly.
        const int rowCount = 1200;

        var first = BuildRows(rowCount, "Visa", "Credit");
        var firstResult = await Run(first);

        firstResult.TotalRows.Should().Be(rowCount);
        firstResult.InsertedCount.Should().Be(rowCount);
        firstResult.RejectedCount.Should().Be(0);

        // Re-importing the identical file must recognise every row as unchanged,
        // which only happens if all batches were found.
        var secondResult = await Run(first);

        secondResult.UnchangedCount.Should().Be(rowCount);
        secondResult.InsertedCount.Should().Be(0);
        secondResult.ConflictCount.Should().Be(0);

        // And a differing re-import must flag every row as a conflict.
        var thirdResult = await Run(BuildRows(rowCount, "Mastercard", "Debit"));

        thirdResult.ConflictCount.Should().Be(rowCount);
        thirdResult.InsertedCount.Should().Be(0);
        thirdResult.UnchangedCount.Should().Be(0);
    }

    private static string[] BuildRows(int count, string scheme, string funding)
    {
        var rows = new string[count];
        for (var i = 0; i < count; i++)
            rows[i] = $"{400000 + i},{scheme},Consumer,{funding},US,2024-01-01,";

        return rows;
    }
}
