using BinTool.Api.Controllers;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Abstractions;
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
    private readonly Mock<IBinRangeAdminService> _admin = new(MockBehavior.Strict);

    private BinRangesController CreateController() => new(_service.Object, _admin.Object);

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

    // ---- Single-range maintenance ----------------------------------------------
    //
    // What the service decided is covered by its own tests; what matters here is that
    // each outcome leaves as the status code that means the same thing.

    [Fact]
    public async Task Get_by_id_returns_404_rather_than_an_empty_200()
    {
        _admin.Setup(a => a.GetAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BinRangeListItem?)null);

        var result = await CreateController().GetById(7, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Creating_returns_201_with_a_location_header()
    {
        var created = Success(BinRangeMutationStatus.Created, 42);

        _admin.Setup(a => a.CreateAsync(It.IsAny<BinRangeInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await CreateController().Create(new BinRangeInput(), CancellationToken.None);

        var route = result.Should().BeOfType<CreatedAtRouteResult>().Subject;
        route.RouteValues!["id"].Should().Be(42);
        route.Value.Should().BeSameAs(created);
    }

    [Fact]
    public async Task Reviving_a_deleted_prefix_returns_200_rather_than_201()
    {
        // Nothing was created, so a Location header pointing at a "new" resource would
        // misdescribe what happened.
        _admin.Setup(a => a.CreateAsync(It.IsAny<BinRangeInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Success(BinRangeMutationStatus.Restored, 42));

        var result = await CreateController().Create(new BinRangeInput(), CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Theory]
    [InlineData(BinRangeMutationStatus.NotFound, typeof(NotFoundObjectResult))]
    [InlineData(BinRangeMutationStatus.PrefixInUse, typeof(ConflictObjectResult))]
    [InlineData(BinRangeMutationStatus.AlreadyInThatState, typeof(ConflictObjectResult))]
    [InlineData(BinRangeMutationStatus.Invalid, typeof(BadRequestObjectResult))]
    public async Task A_refusal_leaves_as_the_matching_status_code(
        BinRangeMutationStatus status, Type expected)
    {
        var refused = BinRangeMutationResult.Failure(status, "no");

        _admin.Setup(a => a.UpdateAsync(1, It.IsAny<BinRangeInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refused);

        var result = await CreateController().Update(1, new BinRangeInput(), CancellationToken.None);

        result.Should().BeOfType(expected);
        result.As<ObjectResult>().Value.Should().BeSameAs(refused,
            "the caller needs the reason, not just the code");
    }

    [Fact]
    public async Task Deleting_and_restoring_return_200_with_the_stored_range()
    {
        _admin.Setup(a => a.DeleteAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Success(BinRangeMutationStatus.Deleted, 3));
        _admin.Setup(a => a.RestoreAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Success(BinRangeMutationStatus.Restored, 3));

        var controller = CreateController();

        (await controller.Delete(3, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.Restore(3, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
    }

    private static BinRangeMutationResult Success(BinRangeMutationStatus status, int id) =>
        BinRangeMutationResult.Success(status, new BinRangeListItem { BinRangeId = id });
}
