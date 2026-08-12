using BinTool.Application.Abstractions;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[Route("api/[controller]")]
public class FundingTypesController : LookupControllerBase
{
    public FundingTypesController(ILookupAdminService service) : base(service) { }

    protected override LookupKind Kind => LookupKind.FundingType;

    protected override string ResourceName => "Funding type";
}
