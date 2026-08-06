using BinTool.Core.Models.ReferenceData;
using BinTool.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Maintains the region reference table (Domestic, Intra-EEA, Inter-Regional, …).
/// A region cannot be deleted while any live country still points at it.
/// </summary>
[Route("api/[controller]")]
public class RegionsController : LookupControllerBase
{
    public RegionsController(ILookupAdminService service) : base(service) { }

    protected override LookupKind Kind => LookupKind.Region;

    protected override string ResourceName => "Region";
}
