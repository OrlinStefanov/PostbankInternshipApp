using BinTool.Application.Abstractions;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>Maintains the product type reference table (Consumer, Commercial, Prepaid).</summary>
[Route("api/[controller]")]
public class ProductTypesController : LookupControllerBase
{
    public ProductTypesController(ILookupAdminService service) : base(service) { }

    protected override LookupKind Kind => LookupKind.ProductType;

    protected override string ResourceName => "Product type";
}
