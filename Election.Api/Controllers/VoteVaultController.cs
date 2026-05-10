using Election.VoteVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace Election.Api.Controllers;

[ApiController]
[Route("api/vault")]
public class VoteVaultController : ControllerBase
{
    private readonly VoteVaultService _vault;

    public VoteVaultController(VoteVaultService vault)
    {
        _vault = vault;
    }

    [HttpPost("custody")]
    public IActionResult CustodyVote([FromBody] object payload)
    {
        var vote = _vault.CustodyVote(payload.ToString()!);

        return Ok(new
        {
            vote.Id,
            vote.Status,
            vote.CreatedAt
        });
    }

    [HttpGet("count")]
    public IActionResult Count()
    {
        return Ok(new
        {
            custodiedVotes = _vault.GetCustodiedVotesCount()
        });
    }

    [HttpGet("votes")]
    public IActionResult Votes()
    {
        try
        {
            return Ok(_vault.GetVotes());
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new
            {
                message = ex.Message
            });
        }
    }
}