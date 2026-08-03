using BinTool.Core.Models.Import;

namespace BinTool.Core.Services;

public interface IBinCsvImportService
{
    /// <summary>
    /// Parses and validates the CSV, splitting it into valid rows and rejected
    /// rows. No database access and nothing is persisted.
    /// </summary>
    /// <param name="csvStream">The uploaded CSV content.</param>
    /// <param name="fileName">Original file name, echoed back in the result.</param>
    BinImportResult BinCsvImport(Stream csvStream, string fileName);
}
