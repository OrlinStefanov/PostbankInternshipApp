using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BinTool.Api.Documentation;

/// <summary>
/// Supplies each operation's long-form description from a markdown file rather than from a
/// <c>&lt;remarks&gt;</c> block in the controller.
/// <para>
/// The prose is the API's manual - sample requests, what a filter means, why a 409 happens -
/// and it ran to three hundred lines across the controllers, which is more of a file than the
/// code it sat above. The overview already lived in ApiDescription.md for the same reason;
/// this is that decision applied per operation. Swagger UI renders it identically.
/// </para>
/// <para>
/// One file per controller under <c>Documentation/Operations</c>, named after the declaring
/// type, with one <c>## MethodName</c> section per action. Actions inherited from a base
/// controller resolve against the base's file, so the four lookup controllers share one.
/// </para>
/// </summary>
public sealed class OperationRemarksFilter : IOperationFilter
{
    private const string ResourcePrefix = "BinTool.Api.Documentation.Operations.";

    // Parsed once per controller and reused: the filter runs for every operation, and the
    // document is built on demand.
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> Cache = new();

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var declaringType = context.MethodInfo.DeclaringType;
        if (declaringType is null) return;

        var sections = Cache.GetOrAdd(declaringType.Name, Load);

        if (sections.TryGetValue(context.MethodInfo.Name, out var description))
        {
            operation.Description = description;
        }
    }

    private static IReadOnlyDictionary<string, string> Load(string controllerName)
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"{ResourcePrefix}{controllerName}.md");

        if (stream is null) return new Dictionary<string, string>();

        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    private static Dictionary<string, string> Parse(string markdown)
    {
        var sections = new Dictionary<string, string>(StringComparer.Ordinal);

        string? current = null;
        var body = new List<string>();

        foreach (var line in markdown.Split('\n'))
        {
            var text = line.TrimEnd('\r');

            if (text.StartsWith("## ", StringComparison.Ordinal))
            {
                Flush();
                current = text[3..].Trim();
                continue;
            }

            if (current is not null) body.Add(text);
        }

        Flush();

        return sections;

        void Flush()
        {
            if (current is null) return;

            var content = string.Join("\n", body).Trim();
            if (content.Length > 0) sections[current] = content;

            body.Clear();
        }
    }
}
