using BinTool.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BinCsvImportController : ControllerBase
{
    private readonly IBinCsvImportService _service;

    public BinCsvImportController(IBinCsvImportService service)
    {
        _service = service;
    }

    /// <summary>
    /// Parses and validates a BIN CSV, returning the valid rows and the rejected
    /// rows (with reasons) side by side. Nothing is saved to the database.
    /// </summary>
    [HttpPost("preview")]
    public IActionResult Preview(IFormFile file)
    {
        if (file == null || file.Length == 0) return BadRequest("Invalid file");

        using var stream = file.OpenReadStream();

        var result = _service.BinCsvImport(stream, file.FileName);

        return Ok(result);
    }
}
