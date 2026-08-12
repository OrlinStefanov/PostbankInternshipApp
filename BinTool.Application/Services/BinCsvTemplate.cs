using System.Text;

namespace BinTool.Application.Services;

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

    private static readonly string[] ExampleRow =
    {
        "400001", "Visa", "Consumer", "Credit", "US", "2024-01-01", string.Empty
    };

    public static byte[] ToCsvBytes()
    {
        var text = new StringBuilder()
            .Append(string.Join(',', Columns)).Append("\r\n")
            .Append(string.Join(',', ExampleRow)).Append("\r\n")
            .ToString();

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text)).ToArray();
    }
}
