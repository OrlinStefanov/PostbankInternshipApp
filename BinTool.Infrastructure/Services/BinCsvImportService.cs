using System.Globalization;
using BinTool.Core.Models.Import;
using BinTool.Core.Services;
using CsvHelper;

namespace BinTool.Infrastructure.Services;

public class BinCsvImportService : IBinCsvImportService
{
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly string[] RequiredColumns =
    {
        "Prefix", "CardScheme", "ProductType", "FundingType", "CountryCode", "ValidFrom"
    };

    public BinImportResult BinCsvImport(Stream csvStream, string fileName)
    {
        var result = new BinImportResult { FileName = fileName };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        // Read the header first so we can (a) validate the columns are present
        // and (b) know whether the optional ValidTo column exists.
        if (!csv.Read() || !csv.ReadHeader())
        {
            AddError(result, 0, "File is empty or has no header row", string.Empty);
            return Finalize(result);
        }

        var header = csv.HeaderRecord ?? Array.Empty<string>();
        var missing = RequiredColumns.Where(c => !header.Contains(c)).ToList();
        if (missing.Count > 0)
        {
            AddError(result, 0, $"Missing required column(s): {string.Join(", ", missing)}",
                string.Join(",", header));
            return Finalize(result);
        }

        var hasValidTo = header.Contains("ValidTo");

        // Track prefixes we have already accepted so a repeat within the same
        // file is rejected rather than silently duplicated.
        var seenPrefixes = new HashSet<string>();
        var rowNumber = 0;

        while (csv.Read())
        {
            rowNumber ++;
            result.TotalRows ++;

            var raw = csv.Parser.RawRecord.Trim();

            var prefix = csv.GetField("Prefix")?.Trim();
            var cardScheme = csv.GetField("CardScheme")?.Trim();
            var productType = csv.GetField("ProductType")?.Trim();
            var fundingType = csv.GetField("FundingType")?.Trim();
            var countryCode = csv.GetField("CountryCode")?.Trim();
            var validFrom = csv.GetField("ValidFrom")?.Trim();
            var validTo = hasValidTo ? csv.GetField("ValidTo")?.Trim() : null;

            if (!TryValidateRow(prefix, cardScheme, productType, fundingType,
                    countryCode, validFrom, validTo, out var row, out var reason))
            {
                AddError(result, rowNumber, reason, raw);
                continue;
            }

            if (!seenPrefixes.Add(row.Prefix!))
            {
                AddError(result, rowNumber,
                    $"Duplicate prefix '{row.Prefix}' already appears earlier in the file", raw);
                continue;
            }

            result.ValidRows.Add(row);
        }

        return Finalize(result);
    }

    /// <summary>
    /// Structural validation only — no lookups against the database yet.
    /// Returns the first rule that fails so the message stays specific.
    /// </summary>
    private static bool TryValidateRow(
        string? prefix, string? cardScheme, string? productType, string? fundingType,
        string? countryCode, string? validFrom, string? validTo,
        out BinImportRow row, out string reason)
    {
        row = new BinImportRow();
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length is < 6 or > 8 || !prefix.All(char.IsDigit))
        {
            reason = "Prefix must be 6-8 digits";
            return false;
        }

        if (string.IsNullOrWhiteSpace(cardScheme)) { reason = "CardScheme is required"; return false; }
        if (string.IsNullOrWhiteSpace(productType)) { reason = "ProductType is required"; return false; }
        if (string.IsNullOrWhiteSpace(fundingType)) { reason = "FundingType is required"; return false; }

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2 || !countryCode.All(char.IsLetter))
        {
            reason = "CountryCode must be a 2-letter ISO code";
            return false;
        }

        if (!DateTime.TryParseExact(validFrom, DateFormat, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var from))
        {
            reason = $"ValidFrom must be a valid date ({DateFormat})";
            return false;
        }

        DateTime? to = null;
        if (!string.IsNullOrWhiteSpace(validTo))
        {
            if (!DateTime.TryParseExact(validTo, DateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsedTo))
            {
                reason = $"ValidTo must be a valid date ({DateFormat})";
                return false;
            }

            if (parsedTo <= from)
            {
                reason = "ValidTo must be after ValidFrom";
                return false;
            }

            to = parsedTo;
        }

        row = new BinImportRow
        {
            Prefix = prefix,
            CardScheme = cardScheme,
            ProductType = productType,
            FundingType = fundingType,
            CountryCode = countryCode,
            ValidFrom = from,
            ValidTo = to
        };
        return true;
    }

    private static void AddError(BinImportResult result, int rowNumber, string reason, string raw) =>
        result.Errors.Add(new BinImportError { RowNumber = rowNumber, Reason = reason, RawData = raw });

    private static BinImportResult Finalize(BinImportResult result)
    {
        result.ValidCount = result.ValidRows.Count;
        result.RejectedCount = result.Errors.Count;
        return result;
    }
}