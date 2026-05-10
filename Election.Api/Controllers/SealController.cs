using Election.VoteVault.Services;
using Microsoft.AspNetCore.Mvc;

namespace Election.Api.Controllers;

[ApiController]
[Route("api/seals")]
public class SealController : ControllerBase
{
    private readonly SealService _sealService;

    public SealController(SealService sealService)
    {
        _sealService = sealService;
    }

    [HttpGet]
    public IActionResult GetSeals()
    {
        return Ok(_sealService.GetSeals());
    }
}