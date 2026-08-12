using System.Text.Json;

namespace BinTool.Application.Models.Audit;

public static class AuditDiff
{
    /// <summary>Pairs the fields of the before and after snapshots.</summary>
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
