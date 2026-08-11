using System.ComponentModel.DataAnnotations;
using BinTool.Application.Models.Commission;
using BinTool.Domain.Common;

namespace BinTool.Application.Validation;

// Checks a rule's own values, before anything is read from storage. The API validates the request
// body before an action runs, so this is only reached by a direct caller - but the rules belong
// with the model, not with whoever remembered to call in correctly.
public static class CommissionRuleValidator
{
    // Returns true when the input is well formed. Otherwise error carries every problem at once:
    // fixing one only to be told about the next is a poor way to fill in a form.
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
