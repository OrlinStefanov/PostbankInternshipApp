using System.Text;
using BinTool.Api.Controllers;
using BinTool.Application.Models.Import;
using BinTool.Application.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BinTool.Tests;

// Request handling in the controller: argument guards and the mapping between the service results
// and HTTP responses. The import rules themselves are covered by the service tests.
public class BinCsvImportControllerTests
{
    private readonly Mock<IBinCsvImportService> _service = new(MockBehavior.Strict);
    private readonly Mock<IImportHistoryQueryService> _history = new(MockBehavior.Strict);

    private BinCsvImportController CreateController() => new(_service.Object, _history.Object);

    private static IFormFile FileWith(string content, string fileName = "bins.csv")
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }

    // ---- Import ----------------------------------------------------------------

    [Fact]
    public async Task Import_returns_BadRequest_when_no_file_is_supplied()
    {
        var result = await CreateController().Import(null!, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_returns_BadRequest_for_an_empty_file()
    {
        var result = await CreateController().Import(FileWith(string.Empty), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Import_returns_the_service_result()
    {
        var expected = new BinImportResult
        {
            FileName = "bins.csv",
            TotalRows = 2,
            InsertedCount = 1,
            ConflictCount = 1
        };

        _service
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), "bins.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateController()
            .Import(FileWith("Prefix\n400001"), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Import_passes_the_uploaded_content_to_the_service()
    {
        const string content = "Prefix,CardScheme\n400001,Visa";
        string? received = null;

        _service
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, string, CancellationToken>((stream, _, _) =>
                received = new StreamReader(stream).ReadToEnd())
            .ReturnsAsync(new BinImportResult());

        await CreateController().Import(FileWith(content), CancellationToken.None);

        received.Should().Be(content);
    }

    [Fact]
    public async Task Import_forwards_the_cancellation_token()
    {
        using var cts = new CancellationTokenSource();

        _service
            .Setup(s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<string>(), cts.Token))
            .ReturnsAsync(new BinImportResult());

        await CreateController().Import(FileWith("Prefix"), cts.Token);

        _service.Verify(
            s => s.ImportAsync(It.IsAny<Stream>(), It.IsAny<string>(), cts.Token), Times.Once);
    }

    // ---- Pending conflicts ------------------------------------------------------

    [Fact]
    public async Task GetConflicts_returns_the_pending_conflicts()
    {
        var expected = new List<BinConflict>
        {
            new() { PendingBinConflictId = 7, Prefix = "400001" }
        };

        _service
            .Setup(s => s.GetPendingConflictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateController().GetConflicts(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetConflicts_returns_Ok_with_an_empty_list_when_nothing_is_pending()
    {
        _service
            .Setup(s => s.GetPendingConflictsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BinConflict>());

        var result = await CreateController().GetConflicts(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeAssignableTo<List<BinConflict>>()
            .Which.Should().BeEmpty();
    }

    // ---- History ----------------------------------------------------------------

    [Fact]
    public async Task GetHistory_returns_the_service_result()
    {
        var query = new ImportHistoryQuery { Status = "Partial" };
        var expected = new BinTool.Application.Models.BinRanges.PagedResult<ImportHistoryItem>
        {
            Items = new List<ImportHistoryItem> { new() { ImportHistoryId = 3, FileName = "bins.csv" } },
            Page = 1,
            PageSize = 25,
            TotalCount = 1
        };

        _history
            .Setup(s => s.SearchAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateController().GetHistory(query, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    // ---- Resolve conflicts -------------------------------------------------------

    [Fact]
    public async Task ResolveConflicts_returns_BadRequest_when_the_body_is_missing()
    {
        var result = await CreateController().ResolveConflicts(null!, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        _service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveConflicts_returns_the_service_result()
    {
        var expected = new ConflictResolutionResult { UpdatedCount = 2, DiscardedCount = 1 };
        var resolutions = new[]
        {
            new ConflictResolution { PendingBinConflictId = 1, Update = true }
        };

        _service
            .Setup(s => s.ResolveConflictsAsync(resolutions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateController().ResolveConflicts(resolutions, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ResolveConflicts_accepts_an_empty_batch()
    {
        var resolutions = Array.Empty<ConflictResolution>();

        _service
            .Setup(s => s.ResolveConflictsAsync(resolutions, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConflictResolutionResult());

        var result = await CreateController().ResolveConflicts(resolutions, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }
}
