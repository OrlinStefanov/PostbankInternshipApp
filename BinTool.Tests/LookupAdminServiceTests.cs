using BinTool.Domain.Entities;
using BinTool.Application.Models.ReferenceData;
using BinTool.Application.Abstractions;
using BinTool.Application.Services;
using BinTool.Infrastructure.Repositories;
using BinTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

/// <summary>
/// Reference-data admin for the four Name+Description tables. Because the four kinds
/// share their shape the tests run as a <see cref="TheoryAttribute"/> over
/// <see cref="LookupKind"/> - a rule that holds for one kind must hold for every kind.
/// </summary>
public class LookupAdminServiceTests : SqliteTestBase
{
    private const string AdminUserId = "admin-user-id";

    private readonly LookupAdminService _service;
    private readonly TestCurrentUser _user;

    public LookupAdminServiceTests()
    {
        SeedUser(AdminUserId, "admin");

        _user = new TestCurrentUser(AdminUserId, "admin");
        _service = new LookupAdminService(
            new LookupRepository(Db), _user, new AuditLog(Db, _user),
            new RecordingLogger<LookupAdminService>());
    }

    public static IEnumerable<object[]> AllKinds => new[]
    {
        new object[] { LookupKind.CardScheme },
        new object[] { LookupKind.ProductType },
        new object[] { LookupKind.FundingType },
        new object[] { LookupKind.Region }
    };

    // ---- Search ---------------------------------------------------------------

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Search_returns_the_seeded_rows_ordered_by_name(LookupKind kind)
    {
        var rows = await _service.SearchAsync(kind, includeDeleted: false);

        rows.Should().NotBeEmpty();
        rows.Should().BeInAscendingOrder(r => r.Name);
        rows.Should().AllSatisfy(r => r.Status.Should().Be(LookupStatus.Active));
    }

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Search_can_include_deleted_rows(LookupKind kind)
    {
        var created = await _service.CreateAsync(kind, new LookupInput { Name = "Temporary" });
        await _service.DeleteAsync(kind, created.Item!.Id);

        var withoutDeleted = await _service.SearchAsync(kind, includeDeleted: false);
        var withDeleted = await _service.SearchAsync(kind, includeDeleted: true);

        withoutDeleted.Should().NotContain(r => r.Name == "Temporary");
        withDeleted.Should().ContainSingle(r => r.Name == "Temporary" && r.Status == LookupStatus.Deleted);
    }

    // ---- Create ---------------------------------------------------------------

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Create_inserts_the_row_and_writes_an_audit_entry(LookupKind kind)
    {
        var result = await _service.CreateAsync(kind, new LookupInput
        {
            Name = "Discover",
            Description = "New scheme for testing"
        });

        result.Succeeded.Should().BeTrue();
        result.Status.Should().Be(LookupMutationStatus.Created);
        result.Item!.Name.Should().Be("Discover");
        result.Item.Status.Should().Be(LookupStatus.Active);

        var audit = NewContext().AuditEntries.Single(a => a.EntityId == result.Item.Id
            && a.EntityType == EntityTypeFor(kind));
        audit.Action.Should().Be(AuditAction.Created);
        audit.PerformedByUserId.Should().Be(AdminUserId);
        audit.OldValues.Should().BeNull();
        audit.NewValues.Should().Contain("Discover");
    }

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Create_refuses_a_duplicate_name_case_insensitively(LookupKind kind)
    {
        await _service.CreateAsync(kind, new LookupInput { Name = "Discover" });

        var result = await _service.CreateAsync(kind, new LookupInput { Name = "DISCOVER" });

        result.Succeeded.Should().BeFalse();
        result.Status.Should().Be(LookupMutationStatus.NameInUse);
    }

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Create_with_a_name_held_by_a_deleted_row_revives_that_row(LookupKind kind)
    {
        var initial = await _service.CreateAsync(kind, new LookupInput { Name = "Discover" });
        var id = initial.Item!.Id;
        await _service.DeleteAsync(kind, id);

        var result = await _service.CreateAsync(kind, new LookupInput
        {
            Name = "Discover",
            Description = "Back with a fresh description"
        });

        result.Succeeded.Should().BeTrue();
        result.Status.Should().Be(LookupMutationStatus.Restored);
        result.Item!.Id.Should().Be(id, "the same row was revived rather than a duplicate created");
        result.Item.Description.Should().Be("Back with a fresh description");
        result.Item.Status.Should().Be(LookupStatus.Active);
    }

    // ---- Update ---------------------------------------------------------------

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Update_overwrites_the_row_and_records_both_sides(LookupKind kind)
    {
        var created = await _service.CreateAsync(kind, new LookupInput
        {
            Name = "Discover",
            Description = "Original"
        });

        var updated = await _service.UpdateAsync(kind, created.Item!.Id, new LookupInput
        {
            Name = "Discover 2.0",
            Description = "Renamed"
        });

        updated.Succeeded.Should().BeTrue();
        updated.Item!.Name.Should().Be("Discover 2.0");

        var audit = NewContext().AuditEntries
            .Where(a => a.EntityId == created.Item.Id && a.EntityType == EntityTypeFor(kind))
            .OrderBy(a => a.AuditEntryId)
            .Last();

        audit.Action.Should().Be(AuditAction.Updated);
        audit.OldValues.Should().Contain("Discover");
        audit.NewValues.Should().Contain("Discover 2.0");
    }

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Update_of_a_deleted_row_is_refused_with_NotFound(LookupKind kind)
    {
        var created = await _service.CreateAsync(kind, new LookupInput { Name = "Discover" });
        await _service.DeleteAsync(kind, created.Item!.Id);

        var result = await _service.UpdateAsync(kind, created.Item.Id, new LookupInput { Name = "Discover Again" });

        result.Status.Should().Be(LookupMutationStatus.NotFound);
    }

    // ---- Delete + restore -----------------------------------------------------

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Delete_soft_deletes_and_records_the_change(LookupKind kind)
    {
        var created = await _service.CreateAsync(kind, new LookupInput { Name = "Discover" });

        var deleted = await _service.DeleteAsync(kind, created.Item!.Id);

        deleted.Succeeded.Should().BeTrue();
        deleted.Item!.Status.Should().Be(LookupStatus.Deleted);
        deleted.Item.DeletedBy.Should().Be("admin");

        var audit = NewContext().AuditEntries.OrderBy(a => a.AuditEntryId).Last(
            a => a.EntityId == created.Item.Id && a.EntityType == EntityTypeFor(kind));

        audit.Action.Should().Be(AuditAction.Deleted);
    }

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Restore_brings_a_deleted_row_back(LookupKind kind)
    {
        var created = await _service.CreateAsync(kind, new LookupInput { Name = "Discover" });
        await _service.DeleteAsync(kind, created.Item!.Id);

        var result = await _service.RestoreAsync(kind, created.Item.Id);

        result.Status.Should().Be(LookupMutationStatus.Restored);
        result.Item!.Status.Should().Be(LookupStatus.Active);
        result.Item.DeletedAt.Should().BeNull();
    }

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Delete_of_an_already_deleted_row_is_AlreadyInThatState(LookupKind kind)
    {
        var created = await _service.CreateAsync(kind, new LookupInput { Name = "Discover" });
        await _service.DeleteAsync(kind, created.Item!.Id);

        var second = await _service.DeleteAsync(kind, created.Item.Id);

        second.Status.Should().Be(LookupMutationStatus.AlreadyInThatState);
    }

    // ---- Referential integrity ------------------------------------------------

    [Fact]
    public async Task Deleting_a_card_scheme_referenced_by_a_live_bin_range_is_refused()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));

        var result = await _service.DeleteAsync(LookupKind.CardScheme, VisaId);

        result.Status.Should().Be(LookupMutationStatus.InUse);
        result.Error.Should().Contain("BIN range");
    }

    [Fact]
    public async Task Deleting_a_region_that_still_has_live_countries_is_refused()
    {
        // Bulgaria (seeded) belongs to Domestic (region id 1), so Domestic cannot leave.
        var result = await _service.DeleteAsync(LookupKind.Region, 1);

        result.Status.Should().Be(LookupMutationStatus.InUse);
        result.Error.Should().Contain("country/countries");
    }

    // ---- Validation -----------------------------------------------------------

    [Theory, MemberData(nameof(AllKinds))]
    public async Task Empty_name_is_rejected(LookupKind kind)
    {
        var result = await _service.CreateAsync(kind, new LookupInput { Name = "" });

        result.Status.Should().Be(LookupMutationStatus.Invalid);
    }

    // ---- Helpers --------------------------------------------------------------

    private static string EntityTypeFor(LookupKind kind) => kind switch
    {
        LookupKind.CardScheme => AuditEntityTypes.CardScheme,
        LookupKind.ProductType => AuditEntityTypes.ProductType,
        LookupKind.FundingType => AuditEntityTypes.FundingType,
        LookupKind.Region => AuditEntityTypes.Region,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(string userId, string name) { UserId = userId; Name = name; }
        public string? UserId { get; }
        public string Name { get; }
    }
}
