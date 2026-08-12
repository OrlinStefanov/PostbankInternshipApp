using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = Permissions.ReferenceDataManage)]
public class CountriesController : ControllerBase
{
    private readonly ICountryAdminService _service;

    public CountriesController(ICountryAdminService service)
    {
        _service = service;
    }

    /// <summary>Lists countries, ordered by ISO code.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<CountryListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var items = await _service.SearchAsync(includeDeleted, cancellationToken);

        return Ok(items);
    }

    /// <summary>Returns one country by id, deleted rows included.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CountryListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Adds a country.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CountryInput input, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(input, cancellationToken);

        if (result.Status == LookupMutationStatus.Created)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Country!.Id }, result);
        }

        return ApiResults.From(result);
    }

    /// <summary>Overwrites a country.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id, [FromBody] CountryInput input, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, input, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>Soft-deletes a country.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>Brings a soft-deleted country back.</summary>
    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CountryMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

}
