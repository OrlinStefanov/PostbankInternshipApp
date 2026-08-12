using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Commission;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

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

    /// <summary>Overwrites a rule with new values.</summary>
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

    /// <summary>Soft-deletes a rule.</summary>
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
    /// </summary>
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
    /// Clears the configured default, so an unmatched classification produces no fee.
    /// </summary>
    [HttpDelete("default")]
    [Authorize(Policy = Permissions.CommissionRulesWrite)]
    [ProducesResponseType(typeof(CommissionRuleMutationResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearDefault(CancellationToken cancellationToken)
    {
        var result = await _service.ClearDefaultAsync(cancellationToken);

        return ApiResults.From(result);
    }

}
