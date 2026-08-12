using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BinTool.Api.Documentation;

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
