using BinTool.Application.Models.Access;

namespace BinTool.Application.Abstractions;

public interface IRoleRepository
{
    Task<IReadOnlyList<RoleRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task<RoleRecord?> FindByIdAsync(string roleId, CancellationToken cancellationToken = default);

    Task<RoleRecord?> FindByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Creates a role and returns it, or the reason the store refused.</summary>
    Task<RoleWriteResult> CreateAsync(
        string name, string? description, CancellationToken cancellationToken = default);

    Task<RoleWriteResult> RenameAsync(
        string roleId, string name, string? description, CancellationToken cancellationToken = default);

    Task<RoleWriteResult> DeleteAsync(string roleId, CancellationToken cancellationToken = default);

    Task SetPermissionsAsync(
        string roleId, IReadOnlySet<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>The permission keys the named roles grant between them, for a login to flatten onto a token.</summary>
    Task<IReadOnlySet<string>> GetPermissionsAsync(
        IEnumerable<string> roleNames, CancellationToken cancellationToken = default);

    /// <summary>Flushes the staged audit entries.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
