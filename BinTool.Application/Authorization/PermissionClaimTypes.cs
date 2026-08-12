namespace BinTool.Application.Authorization;

public static class PermissionClaimTypes
{
    // The same string on both sides - on a role to grant a permission, and on a signed-in user to
    // carry what their roles add up to - is what lets a login flatten role grants into token claims.
    public const string Permission = "permission";
}
