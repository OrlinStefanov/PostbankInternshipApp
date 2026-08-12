using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Currency;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class CurrenciesController : ControllerBase
{
    private readonly ICurrencyService _service;

    public CurrenciesController(ICurrencyService service)
    {
        _service = service;
    }

    /// <summary>Lists currencies, ordered by code.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.CurrenciesRead)]
    [ProducesResponseType(typeof(List<CurrencyListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var items = await _service.SearchAsync(includeDeleted, cancellationToken);

        return Ok(items);
    }

    /// <summary>Returns one currency by id, deleted rows included.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.CurrenciesRead)]
    [ProducesResponseType(typeof(CurrencyListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Adds a currency.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ReferenceDataManage)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CurrencyInput input, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(input, cancellationToken);

        if (result.Status == LookupMutationStatus.Created)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Item!.Id }, result);
        }

        return ApiResults.From(result);
    }

    /// <summary>Overwrites a currency.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.ReferenceDataManage)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id, [FromBody] CurrencyInput input, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, input, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>Soft-deletes a currency.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.ReferenceDataManage)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>Brings a soft-deleted currency back.</summary>
    [HttpPost("{id:int}/restore")]
    [Authorize(Policy = Permissions.ReferenceDataManage)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CurrencyMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

}
