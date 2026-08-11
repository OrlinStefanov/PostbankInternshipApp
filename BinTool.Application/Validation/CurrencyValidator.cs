using System.ComponentModel.DataAnnotations;
using BinTool.Application.Models.Currency;

namespace BinTool.Application.Validation;

public static class CurrencyValidator
{
    public static bool TryValidate(CurrencyInput input, out string error)
    {
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            input, new ValidationContext(input), results, validateAllProperties: true);

        error = string.Join(" ", results.Select(r => r.ErrorMessage));
        return valid;
    }
}
