using BinTool.Application.Models.Audit;
using BinTool.Application.Services;
using BinTool.Domain.Entities;
using FluentAssertions;

namespace BinTool.Tests;

// Filtering, ordering and paging of the audit log browse endpoint. Audit rows are seeded straight
// into the database rather than produced through a writer, so a test can control the timestamps and
// user assignments precisely.
public class AuditQueryServiceTests : SqliteTestBase
{
    private const string AdminId = "admin-user-id";
    private const string ViewerId = "viewer-user-id";

    private static readonly DateTime BaseTime = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly AuditQueryService _service;

    public AuditQueryServiceTests()
    {
        SeedUser(AdminId, "admin");
        SeedUser(ViewerId, "viewer");

        _service = new AuditQueryService(new AuditRepository(Db));
    }

    private AuditEntry Seed(
        AuditAction action = AuditAction.Created,
        string entityType = AuditEntityTypes.BinRange,
        int entityId = 1,
        string? userId = AdminId,
        DateTime? at = null,
        string? oldValues = null,
        string? newValues = "{}")
    {
        var entry = new AuditEntry
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PerformedByUserId = userId,
            PerformedAt = at ?? BaseTime,
            OldValues = oldValues,
            NewValues = newValues
        };

        Db.AuditEntries.Add(entry);
        Db.SaveChanges();
        return entry;
    }

    [Fact]
    public async Task Lists_every_entry_when_no_filter_is_given()
    {
        Seed(entityId: 1);
        Seed(entityId: 2);
        Seed(entityId: 3);

        var result = await _service.SearchAsync(new AuditQuery());

        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Newest_entries_come_first()
    {
        Seed(entityId: 1, at: BaseTime);
        Seed(entityId: 2, at: BaseTime.AddMinutes(5));
        Seed(entityId: 3, at: BaseTime.AddMinutes(10));

        var result = await _service.SearchAsync(new AuditQuery());

        result.Items.Select(i => i.EntityId).Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public async Task Two_entries_at_the_same_instant_break_ties_by_id_descending()
    {
        var first = Seed(entityId: 1, at: BaseTime);
        var second = Seed(entityId: 2, at: BaseTime);

        var result = await _service.SearchAsync(new AuditQuery());

        result.Items.Select(i => i.AuditEntryId)
            .Should().ContainInOrder(second.AuditEntryId, first.AuditEntryId);
    }

    [Fact]
    public async Task From_is_inclusive_and_To_is_exclusive()
    {
        Seed(entityId: 1, at: BaseTime.AddMinutes(-1)); // before the window
        Seed(entityId: 2, at: BaseTime);                // on the boundary
        Seed(entityId: 3, at: BaseTime.AddMinutes(10)); // inside
        Seed(entityId: 4, at: BaseTime.AddHours(1));    // exactly at To - excluded

        var result = await _service.SearchAsync(new AuditQuery
        {
            From = BaseTime,
            To = BaseTime.AddHours(1)
        });

        result.Items.Select(i => i.EntityId).Should().BeEquivalentTo(new[] { 2, 3 });
    }

    [Theory]
    [InlineData("BinRange")]
    [InlineData("binrange")]
    [InlineData("BINRANGE")]
    public async Task Entity_type_filter_is_case_insensitive(string entityType)
    {
        Seed(entityType: AuditEntityTypes.BinRange, entityId: 1);
        Seed(entityType: AuditEntityTypes.Role, entityId: 2);

        var result = await _service.SearchAsync(new AuditQuery { EntityType = entityType });

        result.Items.Should().ContainSingle().Which.EntityId.Should().Be(1);
    }

    [Fact]
    public async Task User_name_filter_matches_the_start_case_insensitively()
    {
        SeedUser("admin2-id", "admin2");
        Seed(entityId: 1, userId: AdminId);
        Seed(entityId: 2, userId: "admin2-id");
        Seed(entityId: 3, userId: ViewerId);

        var result = await _service.SearchAsync(new AuditQuery { UserName = "ADM" });

        result.Items.Select(i => i.EntityId).Should().BeEquivalentTo(new[] { 1, 2 });
    }

    [Fact]
    public async Task Filtering_by_the_literal_system_returns_only_rows_with_no_user()
    {
        Seed(entityId: 1, userId: null);
        Seed(entityId: 2, userId: AdminId);

        var result = await _service.SearchAsync(new AuditQuery { UserName = "system" });

        var item = result.Items.Should().ContainSingle().Subject;
        item.EntityId.Should().Be(1);
        item.UserName.Should().Be("system");
    }

    [Fact]
    public async Task Action_filter_narrows_to_one_action()
    {
        Seed(entityId: 1, action: AuditAction.Created);
        Seed(entityId: 2, action: AuditAction.Updated);
        Seed(entityId: 3, action: AuditAction.Deleted);

        var result = await _service.SearchAsync(new AuditQuery { Action = AuditAction.Updated });

        result.Items.Should().ContainSingle().Which.Action.Should().Be(AuditAction.Updated);
    }

    [Fact]
    public async Task Paging_clamps_page_size_and_returns_the_requested_page()
    {
        for (var i = 1; i <= 5; i++)
        {
            Seed(entityId: i, at: BaseTime.AddMinutes(i));
        }

        var page = await _service.SearchAsync(new AuditQuery { Page = 2, PageSize = 2 });

        page.TotalCount.Should().Be(5);
        page.Page.Should().Be(2);
        page.PageSize.Should().Be(2);
        page.Items.Should().HaveCount(2);
        // Newest first: page 1 is 5,4; page 2 is 3,2.
        page.Items.Select(i => i.EntityId).Should().ContainInOrder(3, 2);
    }

    [Fact]
    public async Task Page_size_is_capped_at_the_maximum()
    {
        Seed(entityId: 1);

        var result = await _service.SearchAsync(new AuditQuery { PageSize = 10_000 });

        result.PageSize.Should().Be(AuditQuery.MaxPageSize);
    }

    [Fact]
    public async Task Empty_result_reports_zero_pages_and_no_items()
    {
        Seed(entityId: 1, at: BaseTime);

        var result = await _service.SearchAsync(new AuditQuery
        {
            From = BaseTime.AddDays(1)
        });

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Entity_types_lists_every_canonical_constant()
    {
        var types = _service.GetEntityTypes();

        types.Should().BeEquivalentTo(new[]
        {
            AuditEntityTypes.BinRange,
            AuditEntityTypes.CardScheme,
            AuditEntityTypes.ProductType,
            AuditEntityTypes.FundingType,
            AuditEntityTypes.Region,
            AuditEntityTypes.Country,
            AuditEntityTypes.CommissionRule,
            AuditEntityTypes.DefaultRule,
            AuditEntityTypes.Role,
            AuditEntityTypes.UserRole
        });
    }
}
