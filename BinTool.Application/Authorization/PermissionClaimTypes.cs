namespace BinTool.Application.Authorization;

public static class PermissionClaimTypes
{
    /// <summary>
    /// The claim type used in two places: on a role (a row in AspNetRoleClaims) to grant it a
    /// permission, and on a signed-in user's principal and token to carry the permissions their
    /// roles add up to. The same string on both sides is what lets a login flatten role grants
    /// into token claims the policies can check.
    /// </summary>
    public const string Permission = "permission";
}
