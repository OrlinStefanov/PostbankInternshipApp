namespace BinTool.Application.Abstractions;

/// <summary>
/// Storage for roles and the permissions they grant.
/// <para>
/// Roles are held by ASP.NET Identity, whose RoleManager and UserManager are persistence
/// types and stay behind this interface. What crosses it is a plain <see cref="RoleRecord"/>,
/// so the service above can be read - and tested - without Identity being present at all.
/// </para>
/// </summary>
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

    /// <summary>
    /// Brings the role's permissions in line with the wanted set - adds what is missing,
    /// removes what is no longer wanted, leaves the rest alone.
    /// </summary>
    Task SetPermissionsAsync(
        string roleId, IReadOnlySet<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>Flushes the staged audit entries. Identity writes its own rows itself.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>A role as everything above the repository sees it.</summary>
public sealed record RoleRecord(
    string Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions,
    int MemberCount);

/// <summary>
/// The outcome of a store write. Identity reports failures as a list of messages rather than
/// by throwing, so they are carried rather than raised.
/// </summary>
public readonly record struct RoleWriteResult(bool Succeeded, string Error, RoleRecord? Role)
{
    public static RoleWriteResult Ok(RoleRecord role) => new(true, string.Empty, role);

    public static RoleWriteResult Refused(string error) => new(false, error, null);
}
