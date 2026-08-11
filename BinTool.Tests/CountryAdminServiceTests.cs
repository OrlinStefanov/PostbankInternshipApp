using BinTool.Application.Abstractions;
using BinTool.Application.Models.ReferenceData;
using BinTool.Application.Services;
using BinTool.Domain.Entities;
using BinTool.Infrastructure.Repositories;
using FluentAssertions;

namespace BinTool.Tests;

// Country CRUD: the parallel of LookupAdminServiceTests for the country reference table, plus the
// rules that are unique to Country - ISO code uppercasing, region resolution, and the
// referential-integrity refusal for a country still named by live BIN ranges.
public class CountryAdminServiceTests : SqliteTestBase
{
    private const string AdminUserId = "admin-user-id";

    private readonly CountryAdminService _service;

    public CountryAdminServiceTests()
    {
        SeedUser(AdminUserId, "admin");

        var user = new TestCurrentUser(AdminUserId, "admin");
        _service = new CountryAdminService(
            new CountryRepository(Db), user, new AuditLog(new AuditRepository(Db), user),
            new RecordingLogger<CountryAdminService>());
    }

    private const int DomesticRegionId = 1;
    private const int InterRegionalRegionId = 3;

    // ---- Create ---------------------------------------------------------------

    [Fact]
    public async Task Create_inserts_the_country_uppercases_the_iso_code_and_audits()
    {
        var result = await _service.CreateAsync(new CountryInput
        {
            IsoCode = "gr",
            Name = "Greece",
            RegionId = DomesticRegionId
        });

        result.Succeeded.Should().BeTrue();
        result.Status.Should().Be(LookupMutationStatus.Created);
        result.Country!.IsoCode.Should().Be("GR");
        result.Country.RegionName.Should().Be("Domestic");

        var audit = NewContext().AuditEntries.Single(a => a.EntityId == result.Country.Id
            && a.EntityType == AuditEntityTypes.Country);
        audit.Action.Should().Be(AuditAction.Created);
        audit.NewValues.Should().Contain("GR").And.Contain("Greece").And.Contain("Domestic");
    }

    [Fact]
    public async Task Create_refuses_a_duplicate_iso_code_case_insensitively()
    {
        var result = await _service.CreateAsync(new CountryInput
        {
            IsoCode = "bg",  // "BG" is already seeded
            Name = "Bulgaria",
            RegionId = DomesticRegionId
        });

        result.Status.Should().Be(LookupMutationStatus.NameInUse);
    }

    [Fact]
    public async Task Create_with_an_unknown_region_is_refused_as_Invalid()
    {
        var result = await _service.CreateAsync(new CountryInput
        {
            IsoCode = "GR",
            Name = "Greece",
            RegionId = 99
        });

        result.Status.Should().Be(LookupMutationStatus.Invalid);
        result.Error.Should().Contain("Region");
    }

    [Fact]
    public async Task Create_with_a_soft_deleted_iso_code_revives_the_row()
    {
        var initial = await _service.CreateAsync(new CountryInput
        {
            IsoCode = "GR",
            Name = "Greece",
            RegionId = DomesticRegionId
        });
        await _service.DeleteAsync(initial.Country!.Id);

        var result = await _service.CreateAsync(new CountryInput
        {
            IsoCode = "GR",
            Name = "Hellenic Republic",
            RegionId = InterRegionalRegionId
        });

        result.Status.Should().Be(LookupMutationStatus.Restored);
        result.Country!.Id.Should().Be(initial.Country.Id);
        result.Country.Name.Should().Be("Hellenic Republic");
        result.Country.RegionId.Should().Be(InterRegionalRegionId);
    }

    // ---- Update ---------------------------------------------------------------

    [Fact]
    public async Task Update_can_change_the_region_and_records_both_sides()
    {
        var created = await _service.CreateAsync(new CountryInput
        {
            IsoCode = "GR",
            Name = "Greece",
            RegionId = InterRegionalRegionId
        });

        var updated = await _service.UpdateAsync(created.Country!.Id, new CountryInput
        {
            IsoCode = "GR",
            Name = "Greece",
            RegionId = DomesticRegionId
        });

        updated.Country!.RegionName.Should().Be("Domestic");

        var audit = NewContext().AuditEntries
            .Where(a => a.EntityId == created.Country.Id && a.EntityType == AuditEntityTypes.Country)
            .OrderBy(a => a.AuditEntryId).Last();
        audit.OldValues.Should().Contain("Inter-Regional");
        audit.NewValues.Should().Contain("Domestic");
    }

    // ---- Delete + restore -----------------------------------------------------

    [Fact]
    public async Task Delete_soft_deletes_the_country()
    {
        var created = await _service.CreateAsync(new CountryInput
        {
            IsoCode = "GR",
            Name = "Greece",
            RegionId = DomesticRegionId
        });

        var deleted = await _service.DeleteAsync(created.Country!.Id);

        deleted.Country!.Status.Should().Be(LookupStatus.Deleted);
    }

    [Fact]
    public async Task Delete_of_a_country_still_referenced_by_a_live_BIN_range_is_refused()
    {
        // United States is seeded with country id 11 and is referenced by the seeded BIN
        // range below - the delete has to refuse rather than orphan the range.
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));

        var result = await _service.DeleteAsync(UsCountryId);

        result.Status.Should().Be(LookupMutationStatus.InUse);
        result.Error.Should().Contain("BIN range");
    }

    [Fact]
    public async Task Restore_brings_a_deleted_country_back()
    {
        var created = await _service.CreateAsync(new CountryInput
        {
            IsoCode = "GR",
            Name = "Greece",
            RegionId = DomesticRegionId
        });
        await _service.DeleteAsync(created.Country!.Id);

        var restored = await _service.RestoreAsync(created.Country.Id);

        restored.Status.Should().Be(LookupMutationStatus.Restored);
        restored.Country!.Status.Should().Be(LookupStatus.Active);
        restored.Country.DeletedAt.Should().BeNull();
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(string userId, string name) { UserId = userId; Name = name; }
        public string? UserId { get; }
        public string Name { get; }
    }
}
