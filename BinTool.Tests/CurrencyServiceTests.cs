using BinTool.Application.Abstractions;
using BinTool.Application.Models.Currency;
using BinTool.Application.Models.ReferenceData;
using BinTool.Application.Services;
using BinTool.Domain.Entities;
using BinTool.Infrastructure.Repositories;
using BinTool.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

public class CurrencyServiceTests : SqliteTestBase
{
    private const string AdminUserId = "admin-user-id";
    private const int EurId = 1, BgnId = 2;

    private readonly CurrencyService _service;
    private readonly TestCurrentUser _user;

    public CurrencyServiceTests()
    {
        SeedUser(AdminUserId, "admin");
        _user = new TestCurrentUser(AdminUserId, "admin");
        _service = new CurrencyService(
            new CurrencyRepository(Db), _user, new AuditLog(Db, _user),
            new RecordingLogger<CurrencyService>());
    }

    [Fact]
    public async Task Search_returns_the_seeded_currencies_ordered_by_code()
    {
        var rows = await _service.SearchAsync();

        rows.Should().Contain(c => c.Code == "EUR" && c.RateToEur == 1m);
        rows.Should().Contain(c => c.Code == "BGN");
        rows.Should().BeInAscendingOrder(c => c.Code);
    }

    [Fact]
    public async Task Create_inserts_the_currency_uppercased_and_audits_it()
    {
        var result = await _service.CreateAsync(new CurrencyInput
        {
            Code = "usd",
            Name = "US dollar",
            RateToEur = 0.92m,
            IsActive = true
        });

        result.Status.Should().Be(LookupMutationStatus.Created);
        result.Item!.Code.Should().Be("USD", "the code is stored upper-case");

        Db.Currencies.Should().Contain(c => c.Code == "USD" && c.RateToEur == 0.92m);
        Db.AuditEntries.AsNoTracking()
            .Should().Contain(a => a.EntityType == AuditEntityTypes.Currency
                                   && a.Action == AuditAction.Created);
    }

    [Fact]
    public async Task Create_rejects_a_code_that_already_exists()
    {
        var result = await _service.CreateAsync(new CurrencyInput
        {
            Code = "EUR",
            Name = "Euro again",
            RateToEur = 1m
        });

        result.Succeeded.Should().BeFalse();
        result.Status.Should().Be(LookupMutationStatus.NameInUse);
    }

    [Theory]
    [InlineData("EU", "Too short")]
    [InlineData("EURO", "Too long")]
    [InlineData("EUR", "Zero rate", 0)]
    public async Task Create_rejects_invalid_input(string code, string name, double rate = 1)
    {
        var result = await _service.CreateAsync(new CurrencyInput
        {
            Code = code,
            Name = name,
            RateToEur = (decimal)rate
        });

        result.Succeeded.Should().BeFalse();
        result.Status.Should().Be(LookupMutationStatus.Invalid);
    }

    [Fact]
    public async Task Update_changes_the_rate_and_audits_it()
    {
        var result = await _service.UpdateAsync(BgnId, new CurrencyInput
        {
            Code = "BGN",
            Name = "Bulgarian lev",
            RateToEur = 0.5m,
            IsActive = true
        });

        result.Succeeded.Should().BeTrue();
        Db.Currencies.Single(c => c.CurrencyId == BgnId).RateToEur.Should().Be(0.5m);
        Db.AuditEntries.AsNoTracking()
            .Should().Contain(a => a.EntityType == AuditEntityTypes.Currency
                                   && a.Action == AuditAction.Updated);
    }

    [Fact]
    public async Task Update_rejects_taking_another_currencys_code()
    {
        var result = await _service.UpdateAsync(BgnId, new CurrencyInput
        {
            Code = "EUR",
            Name = "Bulgarian lev",
            RateToEur = 0.5m
        });

        result.Succeeded.Should().BeFalse();
        result.Status.Should().Be(LookupMutationStatus.NameInUse);
    }

    [Fact]
    public async Task Delete_is_refused_while_a_live_rule_uses_the_currency()
    {
        Db.CommissionRules.Add(new CommissionRule
        {
            RuleName = "Lev rule",
            CurrencyId = BgnId,
            ValidFrom = new DateTime(2024, 1, 1),
            RuleCriteria = new List<RuleCriteria> { new() }
        });
        Db.SaveChanges();

        var result = await _service.DeleteAsync(BgnId);

        result.Succeeded.Should().BeFalse();
        result.Status.Should().Be(LookupMutationStatus.InUse);
    }

    [Fact]
    public async Task Delete_then_restore_round_trips()
    {
        var created = await _service.CreateAsync(new CurrencyInput
        {
            Code = "GBP",
            Name = "Pound sterling",
            RateToEur = 1.17m
        });
        var id = created.Item!.Id;

        (await _service.DeleteAsync(id)).Succeeded.Should().BeTrue();
        Db.Currencies.AsNoTracking().Single(c => c.CurrencyId == id).IsDeleted.Should().BeTrue();

        (await _service.RestoreAsync(id)).Succeeded.Should().BeTrue();
        Db.Currencies.AsNoTracking().Single(c => c.CurrencyId == id).IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Create_revives_a_soft_deleted_code_in_place()
    {
        var created = await _service.CreateAsync(new CurrencyInput
        {
            Code = "CHF",
            Name = "Swiss franc",
            RateToEur = 1.05m
        });
        var id = created.Item!.Id;
        await _service.DeleteAsync(id);

        var revived = await _service.CreateAsync(new CurrencyInput
        {
            Code = "chf",
            Name = "Swiss franc",
            RateToEur = 1.06m
        });

        revived.Status.Should().Be(LookupMutationStatus.Restored);
        revived.Item!.Id.Should().Be(id, "the deleted row is revived, not duplicated");
        revived.Item.RateToEur.Should().Be(1.06m);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public TestCurrentUser(string userId, string name) { UserId = userId; Name = name; }
        public string? UserId { get; }
        public string Name { get; }
    }
}
