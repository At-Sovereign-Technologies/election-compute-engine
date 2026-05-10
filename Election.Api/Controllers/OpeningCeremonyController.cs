using Election.VoteVault.Ceremony.Interfaces;
using Election.VoteVault.Ceremony.Models;
using Microsoft.AspNetCore.Mvc;

namespace Election.Api.Controllers;

[ApiController]
[Route("api/ceremony")]
public class OpeningCeremonyController : ControllerBase
{
    private readonly IOpeningCeremonyService _ceremony;

    public OpeningCeremonyController(
        IOpeningCeremonyService ceremony
    )
    {
        _ceremony = ceremony;
    }

    [HttpPost("open")]
    public IActionResult Open(
        [FromBody] List<KeyShare> shares
    )
    {
        var result = _ceremony.OpenVault(shares);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            vaultOpen = _ceremony.IsVaultOpen()
        });
    }
}