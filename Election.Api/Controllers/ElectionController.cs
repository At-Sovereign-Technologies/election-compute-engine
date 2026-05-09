using Election.Core.Interfaces;
using Election.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Election.Api.Controllers;

[ApiController]
[Route("api/election")]
public class ElectionController : ControllerBase
{
    private readonly IMetodoElectoral _metodo;

    public ElectionController(IMetodoElectoral metodo)
    {
        _metodo = metodo;
    }

    [HttpPost("vote")]
    public IActionResult Vote([FromBody] Voto voto)
    {
        if (!_metodo.ValidarVoto(voto))
        {
            return BadRequest("Voto inválido");
        }

        _metodo.ContabilizarVoto(voto);

        return Ok("Voto registrado");
    }

    [HttpGet("result")]
    public IActionResult Result()
    {
        var resultado = _metodo.CalcularResultado();

        return Ok(resultado);
    }
}