using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

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

    /// <summary>Lists rows for this reference table, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<LookupListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var items = await _service.SearchAsync(Kind, includeDeleted, cancellationToken);

        return Ok(items);
    }

    /// <summary>Returns one row by id, deleted rows included.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(LookupListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(Kind, id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Adds a new row.</summary>
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

    /// <summary>Overwrites a row with new values.</summary>
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

    /// <summary>Soft-deletes a row.</summary>
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
