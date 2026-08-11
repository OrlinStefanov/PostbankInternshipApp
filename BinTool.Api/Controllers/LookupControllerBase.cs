using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// The four Name+Description reference controllers share their shape - the only thing
/// that varies is the <see cref="LookupKind"/>. Each concrete controller inherits from
/// this class and states its kind, so the actions and their XML docs live once here.
/// <para>
/// Everything the controller does needs <c>referencedata.manage</c>. The listing
/// includes soft-deleted rows on demand, which is broader than what the classification
/// filters expose - so reads share the same admin permission as writes.
/// </para>
/// </summary>
[ApiController]
[Produces("application/json")]
[Authorize(Policy = Permissions.ReferenceDataManage)]
public abstract class LookupControllerBase : ControllerBase
{
    private readonly ILookupAdminService _service;

    protected LookupControllerBase(ILookupAdminService service)
    {
        _service = service;
    }

    /// <summary>The reference table this controller manages.</summary>
    protected abstract LookupKind Kind { get; }

    /// <summary>Human-readable name shown in refusal messages.</summary>
    protected abstract string ResourceName { get; }

    /// <summary>
    /// Lists rows for this reference table, ordered by name.
    /// </summary>
    /// <remarks>
    /// By default only live rows are returned. Pass <c>includeDeleted=true</c> to also
    /// receive soft-deleted rows - the only way to find one before restoring it.
    /// </remarks>
    /// <param name="includeDeleted">Include soft-deleted rows.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The rows.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<LookupListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var items = await _service.SearchAsync(Kind, includeDeleted, cancellationToken);

        return Ok(items);
    }

    /// <summary>Returns one row by id, deleted rows included.</summary>
    /// <response code="200">The row.</response>
    /// <response code="404">No row has that id.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LookupListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(Kind, id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>
    /// Adds a new row.
    /// </summary>
    /// <remarks>
    /// Names are case-insensitively unique across live and soft-deleted rows. Naming
    /// a value that exists on a soft-deleted row revives that row in place with the
    /// supplied values and the response comes back as <c>Restored</c> - the row keeps
    /// its id and its audit history.
    /// </remarks>
    /// <response code="201">Added. The body carries the row.</response>
    /// <response code="200">An existing soft-deleted row was revived with these values.</response>
    /// <response code="400">A field broke a rule.</response>
    /// <response code="409">A live row already owns that name.</response>
    [HttpPost]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] LookupInput input, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(Kind, input, cancellationToken);

        if (result.Status == LookupMutationStatus.Created)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Item!.Id }, result);
        }

        return ApiResults.From(result);
    }

    /// <summary>Overwrites a row with new values. A soft-deleted row must be restored first.</summary>
    /// <response code="200">Updated. The body carries the row.</response>
    /// <response code="400">A field broke a rule.</response>
    /// <response code="404">No such row, or it is deleted and must be restored first.</response>
    /// <response code="409">Another row already owns that name.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id, [FromBody] LookupInput input, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(Kind, id, input, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>
    /// Soft-deletes a row. Refused with 409 while the row is still referenced by live
    /// data (BIN ranges for the card scheme / product type / funding type, live
    /// countries for a region).
    /// </summary>
    /// <response code="200">Deleted. The body carries the row.</response>
    /// <response code="404">No row has that id.</response>
    /// <response code="409">The row is already deleted, or is still in use.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(Kind, id, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>Brings a soft-deleted row back.</summary>
    /// <response code="200">Restored. The body carries the row.</response>
    /// <response code="404">No row has that id.</response>
    /// <response code="409">The row was not deleted.</response>
    [HttpPost("{id:int}/restore")]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LookupMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreAsync(Kind, id, cancellationToken);

        return ApiResults.From(result);
    }

}
