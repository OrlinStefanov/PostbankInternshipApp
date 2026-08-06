using BinTool.Core.Models.ReferenceData;
using BinTool.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Maintains the card scheme reference table (Visa, Mastercard, …).
/// </summary>
[Route("api/[controller]")]
public class CardSchemesController : LookupControllerBase
{
    public CardSchemesController(ILookupAdminService service) : base(service) { }

    protected override LookupKind Kind => LookupKind.CardScheme;

    protected override string ResourceName => "Card scheme";
}