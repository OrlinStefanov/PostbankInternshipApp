using System.ComponentModel.DataAnnotations;
using BinTool.Application.Models.Commission;
using BinTool.Domain.Common;

namespace BinTool.Application.Validation;

public static class CommissionRuleValidator
{
    public static bool TryValidate(CommissionRuleInput input, out string error)
    {
        var results = new List<ValidationResult>();
        var messages = new List<string>();

        if (!Validator.TryValidateObject(
                input, new ValidationContext(input), results, validateAllProperties: true))
        {
            messages.AddRange(results.Select(r => r.ErrorMessage!));
        }

        // A cross-field rule the annotations cannot express: an end before the start.
        if (!DateRange.OfDays(input.ValidFrom, input.ValidTo).IsWellFormed)
        {
            messages.Add("ValidTo cannot be earlier than ValidFrom.");
        }

        error = string.Join(" ", messages);
        return messages.Count == 0;
    }
}
