namespace BinTool.Application.Authorization;

// One privilege a role can be granted. The Key is what is stored (as a role claim and, once a user
// signs in, as a claim on their token) and what the authorization policies check; the rest is for
// showing the privilege to a person composing a role.
public sealed record PermissionInfo(string Key, string Name, string Description, string Group);

// The fixed catalog. Each key maps to real enforcement on an endpoint, so the set is defined in
// code - what an admin composes freely is which keys a role holds. Seeding grants Admin every key.
public static class Permissions
{
    public const string BinClassify = "bin.classify";
    public const string CurrenciesRead = "currencies.read";
    public const string BinRangesRead = "binranges.read";
    public const string BinRangesWrite = "binranges.write";
    public const string BinRangesImport = "binranges.import";
    public const string RolesManage = "roles.manage";
    public const string AuditRead = "audit.read";
    public const string ReferenceDataManage = "referencedata.manage";
    public const string CommissionRulesRead = "commissionrules.read";
    public const string CommissionRulesWrite = "commissionrules.write";

    public static class Groups
    {
        public const string Classification = "Classification";
        public const string BinRanges = "BIN ranges";
        public const string Administration = "Administration";
        public const string CommissionRules = "Commission rules";
    }

    public static readonly IReadOnlyList<PermissionInfo> All = new[]
    {
        new PermissionInfo(BinClassify, "Classify BINs",
            "Look up a card BIN against the stored ranges.", Groups.Classification),
        new PermissionInfo(CurrenciesRead, "Read currencies",
            "See the currency list and its euro rates - needed to price a lookup in a currency other than euro, and to read a commission rule's currency.", Groups.Classification),
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

    public static readonly IReadOnlyList<string> AllKeys = All.Select(p => p.Key).ToArray();

    private static readonly HashSet<string> KeySet = new(AllKeys, StringComparer.Ordinal);

    public static bool IsPermission(string key) => KeySet.Contains(key);
}
