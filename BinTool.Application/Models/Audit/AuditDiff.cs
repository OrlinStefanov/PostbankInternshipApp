using System.Text.Json;

namespace BinTool.Application.Models.Audit;

/// <summary>
/// Lines an audit entry's two JSON snapshots up field by field, so a reader sees what
/// actually moved rather than two blocks of JSON to compare by eye.
/// </summary>
public static class AuditDiff
{
    /// <summary>
    /// Pairs the fields of the before and after snapshots.
    /// <para>
    /// A missing snapshot is treated as an empty one, which is what makes a Created entry
    /// come out as all-<see cref="AuditChangeKind.Added"/> and a hard delete as
    /// all-<see cref="AuditChangeKind.Removed"/>. Returns <c>null</c> if either snapshot is
    /// present but is not a JSON object - there is no sound way to pair fields then, and
    /// the caller is expected to fall back to showing the snapshots as they were stored.
    /// </para>
    /// </summary>
    /// <returns>
    /// The fields in the after snapshot's own order, followed by any that exist only in the
    /// before snapshot; or <c>null</c> when the snapshots cannot be paired.
    /// </returns>
    public static IReadOnlyList<AuditFieldChange>? Compute(string? oldValues, string? newValues)
    {
        var before = Flatten(oldValues);
        var after = Flatten(newValues);

        if (before is null || after is null) return null;

        // Last write wins on a duplicated key, matching how a JSON reader would resolve it.
        var beforeLookup = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in before) beforeLookup[key] = value;

        var afterLookup = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in after) afterLookup[key] = value;

        var changes = new List<AuditFieldChange>(afterLookup.Count + beforeLookup.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (key, newValue) in after)
        {
            if (!seen.Add(key)) continue;

            if (beforeLookup.TryGetValue(key, out var oldValue))
            {
                changes.Add(new AuditFieldChange
                {
                    Field = key,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Kind = string.Equals(oldValue, newValue, StringComparison.Ordinal)
                        ? AuditChangeKind.Unchanged
                        : AuditChangeKind.Changed
                });
            }
            else
            {
                changes.Add(new AuditFieldChange
                {
                    Field = key,
                    NewValue = newValue,
                    Kind = AuditChangeKind.Added
                });
            }
        }

        foreach (var (key, oldValue) in before)
        {
            if (afterLookup.ContainsKey(key) || !seen.Add(key)) continue;

            changes.Add(new AuditFieldChange
            {
                Field = key,
                OldValue = oldValue,
                Kind = AuditChangeKind.Removed
            });
        }

        return changes;
    }

    // Reads one snapshot's top-level properties in the order they were written. An absent snapshot
    // flattens to nothing; one that is malformed, or that holds something other than an object,
    // returns null to say it cannot take part in a diff.
    private static List<KeyValuePair<string, string?>>? Flatten(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<KeyValuePair<string, string?>>();

        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;

            var fields = new List<KeyValuePair<string, string?>>();

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                fields.Add(new KeyValuePair<string, string?>(property.Name, Format(property.Value)));
            }

            return fields;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Renders a JSON value as the text shown in the diff. Nested objects and arrays keep their
    // compact JSON: it both displays acceptably and compares correctly, whereas flattening them
    // into more fields would invent names the snapshot never recorded.
    private static string? Format(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => value.GetRawText()
    };
}
