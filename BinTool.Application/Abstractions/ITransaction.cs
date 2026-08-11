namespace BinTool.Application.Abstractions;

/// <summary>
/// A unit of work spanning more than one save. Some writes cannot be done in a single save:
/// an insert has no id until it is stored, and the audit entry describing it has to carry
/// that id - so the two saves are wrapped, and a row that committed without its audit entry
/// becomes impossible rather than merely unlikely.
/// <para>
/// Disposing without committing rolls back, so the <c>await using</c> around one of these
/// is what makes an early return safe.
/// </para>
/// </summary>
public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
