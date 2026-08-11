using System.ComponentModel.DataAnnotations;
using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Validation;

public static class LookupValidator
{
    public static bool TryValidate(LookupInput input, out string error)
    {
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            input, new ValidationContext(input), results, validateAllProperties: true);

        error = string.Join(" ", results.Select(r => r.ErrorMessage));
        return valid;
    }
}
