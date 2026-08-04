using BinTool.Api.Controllers;
using BinTool.Core.Models.BinRanges;
using BinTool.Core.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BinTool.Tests;

/// <summary>
/// The controller's contract: what it forwards to the service and what it returns.
/// The filtering and paging rules are covered by the service tests.
/// </summary>
public class BinRangesControllerTests
{
    private readonly Mock<IBinRangeQueryService> _service = new(MockBehavior.Strict);

    private BinRangesController CreateController() => new(_service.Object);

    [Fact]
    public async Task Search_forwards_the_query_and_returns_the_result()
    {
        var query = new BinRangeQuery { Prefix = "4000", Page = 2 };
        var expected = new PagedResult<BinRangeListItem> { Page = 2, PageSize = 25, TotalCount = 30 };

        _service
            .Setup(s => s.SearchAsync(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateController().Search(query, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Search_returns_200_when_nothing_matched()
    {
        _service
            .Setup(s => s.SearchAsync(It.IsAny<BinRangeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<BinRangeListItem>());

        var result = await CreateController().Search(new BinRangeQuery(), CancellationToken.None);

        var value = result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeOfType<PagedResult<BinRangeListItem>>().Subject;

        value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Filters_returns_the_options()
    {
        var expected = new BinRangeFilterOptions { CardSchemes = { "Visa" } };

        _service
            .Setup(s => s.GetFilterOptionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateController().Filters(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeSameAs(expected);
    }
}
