namespace BinTool.Application.Models.Common;

public interface IMutationResult
{
    MutationOutcome Outcome { get; }

    string? Error { get; }
}
