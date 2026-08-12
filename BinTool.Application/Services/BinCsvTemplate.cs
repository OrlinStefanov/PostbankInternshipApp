using System.Text;

namespace BinTool.Application.Services;

// The header BinCsvImportService reads, kept here so the file handed out as a template and the file
// the importer accepts are the same list rather than two lists that agree today.
public static class BinCsvTemplate
{
    public const string FileName = "bin_import_template.csv";

    public const string ContentType = "text/csv";

    public static readonly string[] Columns =
    {
        "Prefix", "CardScheme", "ProductType", "FundingType", "CountryCode", "ValidFrom", "ValidTo"
    };

    // ValidTo is the one column a row may leave out entirely, so the required set is the rest.
    public static readonly string[] RequiredColumns = Columns[..^1];

    // One row under the header showing each column in the form the importer accepts: a 6-8 digit
    // prefix, reference values by name, a 2-letter ISO country code and yyyy-MM-dd dates, with
    // ValidTo blank for an open-ended range.
    private static readonly string[] ExampleRow =
    {
        "400001", "Visa", "Consumer", "Credit", "US", "2024-01-01", string.Empty
    };

    // CRLF and a byte-order mark, because this file is opened in Excel before it is filled in. The
    // importer reads the mark back off, so what is downloaded here imports unchanged.
    public static byte[] ToCsvBytes()
    {
        var text = new StringBuilder()
            .Append(string.Join(',', Columns)).Append("\r\n")
            .Append(string.Join(',', ExampleRow)).Append("\r\n")
            .ToString();

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
    }
}
