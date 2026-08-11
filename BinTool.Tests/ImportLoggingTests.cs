using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace BinTool.Tests;

/// <summary>
/// What an import run leaves in the log. A 1000-row file cannot log per row without drowning
/// everything else, so what is asserted here is that the run is traceable as a run: one scope
/// carrying the run id, a summary carrying the counts as fields, and a warning when the run
/// left work behind.
/// </summary>
public class ImportLoggingTests : ImportTestBase
{
    [Fact]
    public async Task A_run_opens_one_scope_carrying_the_run_id_and_the_file_name()
    {
        await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        var scope = ImportLogger.Scopes.Should().ContainSingle().Subject
            .Should().BeAssignableTo<IReadOnlyDictionary<string, object>>().Subject;

        scope.Should().ContainKey("ImportRunId");
        scope["ImportFile"].Should().Be("test.csv");
    }

    [Fact]
    public async Task The_summary_reports_the_counts_as_fields()
    {
        await Run(
            "400001,Visa,Consumer,Credit,US,2024-01-01,",
            "400002,Visa,Consumer,Credit,US,2024-01-01,",
            "bad row");

        var summary = ImportLogger.Entries.Single(e => e.EventId.Id == 2002);

        summary.Level.Should().Be(LogLevel.Information);
        summary.Fields["TotalRows"].Should().Be(3);
        summary.Fields["InsertedRows"].Should().Be(2);
        summary.Fields["RejectedRows"].Should().Be(1);
        summary.Fields["Status"].Should().Be("Partial");
    }

    [Fact]
    public async Task A_file_with_no_usable_header_is_logged_as_a_warning_not_an_error()
    {
        await RunRaw("Prefix,CardScheme\n400001,Visa");

        var entry = ImportLogger.Entries.Single(e => e.EventId.Id == 2003);

        entry.Level.Should().Be(LogLevel.Warning);
        entry.Exception.Should().BeNull();
        entry.Message.Should().Contain("required column(s) missing");
    }

    [Fact]
    public async Task A_run_that_leaves_conflicts_pending_warns_separately_from_its_summary()
    {
        await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");
        ImportLogger.Entries.Clear();

        await Run("400001,Mastercard,Consumer,Debit,US,2024-01-01,");

        // The run finished, so it still reports Information; the outstanding decision is a
        // second, separate event, because "done" and "done but incomplete" are not the same
        // thing to whoever reads the log.
        ImportLogger.Entries.Should().Contain(e => e.EventId.Id == 2002 && e.Level == LogLevel.Information);

        var pending = ImportLogger.Entries.Single(e => e.EventId.Id == 2004);
        pending.Level.Should().Be(LogLevel.Warning);
        pending.Fields["ConflictCount"].Should().Be(1);
    }

    [Fact]
    public async Task No_log_message_repeats_a_rejected_row_verbatim()
    {
        await Run("400001,NoSuchScheme,Consumer,Credit,US,2024-01-01,");

        // The rejected line goes to the result and the rejection rows, where the user can
        // read it. It is unvalidated caller input, so it does not go to the log.
        ImportLogger.Entries.Should().NotContain(e => e.Message.Contains("NoSuchScheme"));
    }
}
