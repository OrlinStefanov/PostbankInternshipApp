using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Currency;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Maintains the currency table and the euro rates commission is priced and displayed in.
/// A currency is a code, a name and the euro value of one unit; euro is the base at rate 1.
/// <para>
/// Reading and maintaining are separate privileges, unlike the other reference-data tables.
/// The currency list is not only an admin's to edit: anyone pricing a lookup has to choose
/// the currency the amount is in, and anyone reading a commission rule sees the currency it
/// is denominated in. So the two reads take <c>currencies.read</c> and the writes take
/// <c>referencedata.manage</c>, stated per action rather than once on the class.
/// </para>
/// <para>
/// A currency still used by a live commission rule cannot be deleted. Deletes are soft.
/// </para>
/// </summary>
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

    /// <summary>Lists currencies, ordered by code. Soft-deleted rows only when asked.</summary>
    /// <param name="includeDeleted">Include soft-deleted rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The currencies.</response>
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
    /// <response code="200">The currency.</response>
    /// <response code="404">No currency has that id.</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.CurrenciesRead)]
    [ProducesResponseType(typeof(CurrencyListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Adds a currency. A code already held by a soft-deleted row revives that row in place
    /// (response <c>Restored</c>).
    /// </summary>
    /// <response code="201">Added. The body carries the currency.</response>
    /// <response code="200">An existing soft-deleted row was revived with these values.</response>
    /// <response code="400">A field broke a rule.</response>
    /// <response code="409">A live row already owns that code.</response>
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

    /// <summary>Overwrites a currency. A soft-deleted row must be restored first.</summary>
    /// <response code="200">Updated. The body carries the currency.</response>
    /// <response code="400">A field broke a rule.</response>
    /// <response code="404">No such row, or it is deleted and must be restored first.</response>
    /// <response code="409">Another row already owns that code.</response>
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

    /// <summary>
    /// Soft-deletes a currency. Refused with 409 while a live commission rule still uses it.
    /// </summary>
    /// <response code="200">Deleted. The body carries the currency.</response>
    /// <response code="404">No currency has that id.</response>
    /// <response code="409">The row is already deleted, or is still in use.</response>
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
    /// <response code="200">Restored. The body carries the currency.</response>
    /// <response code="404">No currency has that id.</response>
    /// <response code="409">The row was not deleted.</response>
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
