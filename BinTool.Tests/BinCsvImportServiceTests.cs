using System.Text;
using BinTool.Core.Services;
using BinTool.Infrastructure.Services;
using FluentAssertions;

namespace BinTool.Tests;

public class BinCsvImportServiceTests
{
    private const string Header =
        "Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom,ValidTo";

    private readonly IBinCsvImportService _service = new BinCsvImportService();

    /// <summary>
    /// Builds an in-memory CSV from a header plus the given data rows and runs it
    /// through the service.
    /// </summary>
    private BinTool.Core.Models.Import.BinImportResult Run(params string[] dataRows)
    {
        var content = new StringBuilder().AppendLine(Header);
        foreach (var row in dataRows)
            content.AppendLine(row);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content.ToString()));
        return _service.BinCsvImport(stream, "test.csv");
    }

    [Fact]
    public void Valid_row_is_accepted_and_parsed()
    {
        var result = Run("400001,Visa,Consumer,Credit,US,2024-01-01,2026-12-31");

        result.ValidCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);

        var row = result.ValidRows.Single();
        row.Prefix.Should().Be("400001");
        row.CardScheme.Should().Be("Visa");
        row.ProductType.Should().Be("Consumer");
        row.FundingType.Should().Be("Credit");
        row.CountryCode.Should().Be("US");
        row.ValidFrom.Should().Be(new DateTime(2024, 1, 1));
        row.ValidTo.Should().Be(new DateTime(2026, 12, 31));
    }

    [Fact]
    public void Blank_ValidTo_is_accepted_as_open_ended()
    {
        var result = Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        result.ValidCount.Should().Be(1);
        result.ValidRows.Single().ValidTo.Should().BeNull();
    }

    [Theory]
    [InlineData("123456")]   // 6 digits - lower boundary
    [InlineData("12345678")] // 8 digits - upper boundary
    public void Prefix_at_length_boundaries_is_accepted(string prefix)
    {
        var result = Run($"{prefix},Visa,Consumer,Credit,US,2024-01-01,");

        result.ValidCount.Should().Be(1);
        result.RejectedCount.Should().Be(0);
    }

    [Theory]
    [InlineData("12345")]      // too short
    [InlineData("123456789")]  // too long
    [InlineData("12A456")]     // non-numeric
    [InlineData("")]           // empty
    public void Invalid_prefix_is_rejected(string prefix)
    {
        var result = Run($"{prefix},Visa,Consumer,Credit,US,2024-01-01,");

        result.ValidCount.Should().Be(0);
        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("Prefix must be 6-8 digits");
    }

    [Theory]
    [InlineData("400001,,Consumer,Credit,US,2024-01-01,", "CardScheme is required")]
    [InlineData("400001,Visa,,Credit,US,2024-01-01,", "ProductType is required")]
    [InlineData("400001,Visa,Consumer,,US,2024-01-01,", "FundingType is required")]
    public void Missing_required_lookup_field_is_rejected(string row, string expectedReason)
    {
        var result = Run(row);

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be(expectedReason);
    }

    [Theory]
    [InlineData("USA")] // 3 letters
    [InlineData("U")]   // 1 letter
    [InlineData("U1")]  // contains a digit
    public void Invalid_country_code_is_rejected(string code)
    {
        var result = Run($"400001,Visa,Consumer,Credit,{code},2024-01-01,");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("CountryCode must be a 2-letter ISO code");
    }

    [Fact]
    public void Malformed_ValidFrom_is_rejected_without_failing_the_batch()
    {
        var result = Run(
            "400001,Visa,Consumer,Credit,US,2024-13-99,",   // invalid month/day
            "400002,Visa,Consumer,Debit,BG,2024-01-01,");   // valid

        result.ValidCount.Should().Be(1);
        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("ValidFrom must be a valid date (yyyy-MM-dd)");
        result.Errors.Single().RowNumber.Should().Be(1);
    }

    [Fact]
    public void ValidTo_before_ValidFrom_is_rejected()
    {
        var result = Run("400001,Visa,Consumer,Credit,US,2024-01-01,2023-01-01");

        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Be("ValidTo must be after ValidFrom");
    }

    [Fact]
    public void Duplicate_prefix_within_file_rejects_the_later_row()
    {
        var result = Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",
            "400001,Mastercard,Commercial,Debit,BG,2024-01-01,");

        result.ValidCount.Should().Be(1);
        result.RejectedCount.Should().Be(1);
        result.Errors.Single().Reason.Should().Contain("Duplicate prefix '400001'");
        result.Errors.Single().RowNumber.Should().Be(2);
    }

    [Fact]
    public void Missing_required_column_returns_a_single_header_error()
    {
        // No ProductType column in the header.
        var csv = "Prefix,CardScheme,FundingType,CountryCode,ValidFrom,ValidTo\n" +
                  "400001,Visa,Credit,US,2024-01-01,\n";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = _service.BinCsvImport(stream, "bad.csv");

        result.ValidCount.Should().Be(0);
        result.TotalRows.Should().Be(0); // bails out before reading data rows
        result.Errors.Single().Reason.Should().Contain("Missing required column(s)");
        result.Errors.Single().Reason.Should().Contain("ProductType");
    }

    [Fact]
    public void Empty_file_is_reported_as_error()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

        var result = _service.BinCsvImport(stream, "empty.csv");

        result.ValidCount.Should().Be(0);
        result.Errors.Single().Reason.Should().Be("File is empty or has no header row");
    }

    [Fact]
    public void Result_echoes_filename_and_counts_are_consistent()
    {
        var result = Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",  // valid
            "12345,Visa,Consumer,Credit,US,2024-01-01,");  // invalid prefix

        result.FileName.Should().Be("test.csv");
        result.TotalRows.Should().Be(2);
        result.ValidCount.Should().Be(result.ValidRows.Count);
        result.RejectedCount.Should().Be(result.Errors.Count);
        (result.ValidCount + result.RejectedCount).Should().Be(result.TotalRows);
    }
}
