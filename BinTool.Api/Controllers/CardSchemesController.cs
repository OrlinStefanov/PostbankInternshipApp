using BinTool.Application.Abstractions;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[Route("api/[controller]")]
public class CardSchemesController : LookupControllerBase
{
    public CardSchemesController(ILookupAdminService service) : base(service) { }

    protected override LookupKind Kind => LookupKind.CardScheme;

    protected override string ResourceName => "Card scheme";
}
