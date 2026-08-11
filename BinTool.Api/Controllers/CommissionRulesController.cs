using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Commission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Maintains commission rules - the pricing an admin changes without a developer or a
/// release. A rule is a percentage plus a fixed amount with a minimum fee, keyed on a
/// scheme/product/funding/region combination where any field may be a wildcard.
/// <para>
/// Reads take <c>commissionrules.read</c>; writes take <c>commissionrules.write</c>. Saving a
/// rule that overlaps an existing one on the same key is refused with 409, and the response
/// names the conflicting rule. Deletes are soft; the configured default cannot be deleted
/// until another rule takes its place.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class CommissionRulesController : ControllerBase
{
    private readonly ICommissionRuleAdminService _service;

    public CommissionRulesController(ICommissionRuleAdminService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lists commission rules, active ones first, then by specificity and priority.
    /// </summary>
    /// <param name="includeDeleted">Include soft-deleted rules.</param>
    /// <param name="includeExpired">Include rules whose validity has passed.</param>
    /// <response code="200">The rules.</response>
    [HttpGet]
    [Authorize(Policy = Permissions.CommissionRulesRead)]
    [ProducesResponseType(typeof(List<CommissionRuleListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool includeExpired = false,
        CancellationToken cancellationToken = default)
    {
        var items = await _service.SearchAsync(includeDeleted, includeExpired, cancellationToken);

        return Ok(items);
    }

    /// <summary>Returns one rule by id, deleted rules included.</summary>
    /// <response code="200">The rule.</response>
    /// <response code="404">No rule has that id.</response>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.CommissionRulesRead)]
    [ProducesResponseType(typeof(CommissionRuleListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetAsync(id, cancellationToken);

        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Adds a rule.</summary>
    /// <response code="201">Added. The body carries the rule.</response>
    /// <response code="400">A field broke a rule, or a key id does not exist.</response>
    /// <response code="409">The validity overlaps an existing rule on the same key.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.CommissionRulesWrite)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CommissionRuleInput input, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(input, cancellationToken);

        if (result.Status == CommissionRuleMutationStatus.Created)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Rule!.Id }, result);
        }

        return ApiResults.From(result);
    }

    /// <summary>Overwrites a rule with new values. A soft-deleted rule must be restored first.</summary>
    /// <response code="200">Updated. The body carries the rule.</response>
    /// <response code="400">A field broke a rule, or a key id does not exist.</response>
    /// <response code="404">No such rule, or it is deleted and must be restored first.</response>
    /// <response code="409">The validity overlaps another rule on the same key.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.CommissionRulesWrite)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id, [FromBody] CommissionRuleInput input, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, input, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>
    /// Soft-deletes a rule. Refused with 409 while the rule is the configured default -
    /// set another rule as the default, or clear it, first.
    /// </summary>
    /// <response code="200">Deleted. The body carries the rule.</response>
    /// <response code="404">No rule has that id.</response>
    /// <response code="409">The rule is already deleted, or it is the current default.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.CommissionRulesWrite)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _service.DeleteAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>Brings a soft-deleted rule back.</summary>
    /// <response code="200">Restored. The body carries the rule.</response>
    /// <response code="404">No rule has that id.</response>
    /// <response code="409">The rule was not deleted.</response>
    [HttpPost("{id:int}/restore")]
    [Authorize(Policy = Permissions.CommissionRulesWrite)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var result = await _service.RestoreAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>
    /// Makes a rule the fallback default, applied when no rule matches a classified card.
    /// Replaces whatever rule was default before.
    /// </summary>
    /// <response code="200">The rule is now the default.</response>
    /// <response code="400">The rule is soft-deleted or inactive and cannot be the default.</response>
    /// <response code="404">No rule has that id.</response>
    [HttpPost("{id:int}/default")]
    [Authorize(Policy = Permissions.CommissionRulesWrite)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefault(int id, CancellationToken cancellationToken)
    {
        var result = await _service.SetDefaultAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>
    /// Clears the configured default, so an unmatched classification produces no fee. A no-op
    /// that still succeeds when there was no default configured.
    /// </summary>
    /// <response code="200">There is no default rule.</response>
    [HttpDelete("default")]
    [Authorize(Policy = Permissions.CommissionRulesWrite)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearDefault(CancellationToken cancellationToken)
    {
        var result = await _service.ClearDefaultAsync(cancellationToken);

        return ApiResults.From(result);
    }

}
