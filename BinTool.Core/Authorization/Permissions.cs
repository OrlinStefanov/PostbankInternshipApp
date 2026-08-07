namespace BinTool.Core.Authorization;

/// <summary>
/// One privilege a role can be granted. The <see cref="Key"/> is what is stored (as a role
/// claim and, once a user signs in, as a claim on their token) and what the authorization
/// policies check; the rest is for showing the privilege to a person composing a role.
/// </summary>
public sealed record PermissionInfo(string Key, string Name, string Description, string Group);

/// <summary>
/// The fixed catalog of privileges. Each key maps to real enforcement on an endpoint, so the
/// set is defined in code - what an admin composes freely is which of these a role holds, and
/// which roles a user holds.
/// <para>
/// Adding a privilege is: add a constant and a <see cref="PermissionInfo"/> here, and put the
/// key on an endpoint. Seeding grants the Admin role every key, so Admin picks it up
/// automatically, and the role editor renders it from <see cref="All"/> with no further work.
/// </para>
/// </summary>
public static class Permissions
{
    public const string BinClassify = "bin.classify";
    public const string BinRangesRead = "binranges.read";
    public const string BinRangesWrite = "binranges.write";
    public const string BinRangesImport = "binranges.import";
    public const string RolesManage = "roles.manage";
    public const string AuditRead = "audit.read";
    public const string ReferenceDataManage = "referencedata.manage";
    public const string CommissionRulesRead = "commissionrules.read";
    public const string CommissionRulesWrite = "commissionrules.write";

    /// <summary>Headings the role editor groups the privileges under.</summary>
    public static class Groups
    {
        public const string Classification = "Classification";
        public const string BinRanges = "BIN ranges";
        public const string Administration = "Administration";
        public const string CommissionRules = "Commission rules";
    }

    /// <summary>Every privilege, in the order a role editor should present them.</summary>
    public static readonly IReadOnlyList<PermissionInfo> All = new[]
    {
        new PermissionInfo(BinClassify, "Classify BINs",
            "Look up a card BIN against the stored ranges.", Groups.Classification),
        new PermissionInfo(BinRangesRead, "Browse BIN ranges",
            "View the stored BIN ranges, their filters and a single range.", Groups.BinRanges),
        new PermissionInfo(BinRangesWrite, "Maintain BIN ranges",
            "Add, edit, delete and restore a BIN range by hand.", Groups.BinRanges),
        new PermissionInfo(BinRangesImport, "Import BIN ranges",
            "Upload a CSV and resolve import conflicts.", Groups.BinRanges),
        new PermissionInfo(RolesManage, "Manage access",
            "Create roles, set their permissions, and assign roles to users.", Groups.Administration),
        new PermissionInfo(AuditRead, "View audit log",
            "Read the trail of who changed which record and when.", Groups.Administration),
        new PermissionInfo(ReferenceDataManage, "Manage reference data",
            "Add, edit, deactivate and restore card schemes, product types, funding types, regions and countries.", Groups.Administration),
        new PermissionInfo(CommissionRulesRead, "Browse commission rules",
            "View the stored commission rules, their key combination, rates and validity.", Groups.CommissionRules),
        new PermissionInfo(CommissionRulesWrite, "Maintain commission rules",
            "Add, edit, deactivate, restore and set the default commission rule.", Groups.CommissionRules)
    };

    /// <summary>The keys alone, for seeding the Admin role with the whole catalog.</summary>
    public static readonly IReadOnlyList<string> AllKeys = All.Select(p => p.Key).ToArray();

    private static readonly HashSet<string> KeySet = new(AllKeys, StringComparer.Ordinal);

    /// <summary>Whether a string is one of the catalog keys - used by the policy provider to
    /// tell a permission policy name apart from any other policy.</summary>
    public static bool IsPermission(string key) => KeySet.Contains(key);
}