using BinTool.Application.Abstractions;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[Route("api/[controller]")]
public class RegionsController : LookupControllerBase
{
    public RegionsController(ILookupAdminService service) : base(service) { }

    protected override LookupKind Kind => LookupKind.Region;

    protected override string ResourceName => "Region";
}
