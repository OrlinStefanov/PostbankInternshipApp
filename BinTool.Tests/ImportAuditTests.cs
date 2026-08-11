using BinTool.Application.Abstractions;
using BinTool.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Tests;

public class ImportAuditTests : ImportTestBase
{
    private const string UserId = "user-1";

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string? UserId { get; init; }

        public string Name { get; init; } = ICurrentUser.SystemName;
    }

    private void SignIn(string userName)
    {
        Db.Users.Add(new ApplicationUser
        {
            Id = UserId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@bintool.local",
            FullName = "Test User"
        });
        Db.SaveChanges();

        CurrentUser = new FakeCurrentUser { UserId = UserId, Name = userName };
    }

    [Fact]
    public async Task An_inserted_range_records_who_imported_it()
    {
        SignIn("admin");

        await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        var range = await Db.BinRanges.AsNoTracking().SingleAsync();
        range.CreatedBy.Should().Be("admin");
        range.UpdatedBy.Should().Be("admin");
    }

    [Fact]
    public async Task The_import_history_is_linked_to_the_user_account()
    {
        SignIn("admin");

        var result = await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        var history = await Db.ImportHistories.AsNoTracking()
            .SingleAsync(h => h.ImportHistoryId == result.ImportHistoryId);

        history.ImportedByUserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Resolving_a_conflict_records_who_decided()
    {
        SeedBinRange("400001", VisaId, ConsumerId, CreditId, UsCountryId, new DateTime(2024, 1, 1));
        SignIn("admin");

        var imported = await Run("400001,Visa,Commercial,Credit,US,2024-01-01,");
        var conflictId = imported.Conflicts.Single().PendingBinConflictId;

        await Service.ResolveConflictsAsync(new[]
        {
            new Application.Models.Import.ConflictResolution { PendingBinConflictId = conflictId, Update = true }
        });

        var conflict = await Db.PendingBinConflicts.AsNoTracking().SingleAsync();
        conflict.ResolvedBy.Should().Be("admin");

        var range = await Db.BinRanges.AsNoTracking().SingleAsync();
        range.UpdatedBy.Should().Be("admin");
    }

    [Fact]
    public async Task Without_a_signed_in_user_the_audit_falls_back_to_system()
    {
        await Run("400001,Visa,Consumer,Credit,US,2024-01-01,");

        var range = await Db.BinRanges.AsNoTracking().SingleAsync();
        range.CreatedBy.Should().Be("system");

        var history = await Db.ImportHistories.AsNoTracking().SingleAsync();
        history.ImportedByUserId.Should().BeNull();
    }
}
