using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Maintains the country reference table. Unlike the named lookups, a country carries
/// an ISO 3166-1 alpha-2 code and a region assignment - so it has its own controller.
/// <para>
/// A country cannot be deleted while any live BIN range still names it as the issuing
/// country. Restore is unconditional.
/// </para>
/// </summary>
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
    /// <param name="includeDeleted">Include soft-deleted rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The rows.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CountryListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var items = await _service.SearchAsync(includeDeleted, cancellationToken);

        return Ok(items);
    }

    /// <summary>Returns one country by id, deleted rows included.</summary>
    /// <response code="200">The country.</response>
    /// <response code="404">No country has that id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CountryListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Adds a country. The ISO code is uppercased and must be free across live and
    /// soft-deleted rows; a code already held by a soft-deleted row revives that row in
    /// place with the supplied values.
    /// </summary>
    /// <response code="201">Added.</response>
    /// <response code="200">An existing soft-deleted country was revived with these values.</response>
    /// <response code="400">Validation failed, or the region does not exist.</response>
    /// <response code="409">A live country already owns that ISO code.</response>
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

    /// <summary>Overwrites a country. A soft-deleted country must be restored first.</summary>
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

    /// <summary>
    /// Soft-deletes a country. Refused with 409 while any live BIN range still names it.
    /// </summary>
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
