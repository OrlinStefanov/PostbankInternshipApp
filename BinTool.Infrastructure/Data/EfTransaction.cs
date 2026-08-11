using Microsoft.EntityFrameworkCore.Storage;

namespace BinTool.Infrastructure.Data;

/// <summary>
/// Adapts EF Core's transaction to the <see cref="ITransaction"/> the application layer is
/// written against, so a service can span two saves without naming a persistence type.
/// </summary>
internal sealed class EfTransaction : ITransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        _transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
