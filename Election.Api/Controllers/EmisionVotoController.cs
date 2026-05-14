using System.ComponentModel.DataAnnotations;
using Election.Core.Interfaces;
using Election.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Election.Api.Controllers;

[ApiController]
[Route("api/v1/emision")]
public class EmisionVotoController : ControllerBase
{
    private readonly IServicioEmisionVoto _servicio;
    private readonly ILogger<EmisionVotoController> _logger;

    public EmisionVotoController(
        IServicioEmisionVoto servicio,
        ILogger<EmisionVotoController> logger)
    {
        _servicio = servicio;
        _logger = logger;
    }

    [HttpPost("presencial")]
    public ActionResult<ComprobanteVoto> EmitirPresencial([FromBody] EmisionVoto emision)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            emision.Canal = CanalVoto.Presencial;
            var comprobante = _servicio.EmitirPresencial(emision, IpRemoto(), UserAgent());
            return Ok(comprobante);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "EmisionPresencial: payload inválido.");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "EmisionPresencial: voto rechazado por el método electoral.");
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    [HttpPost("remoto")]
    public ActionResult<ComprobanteVoto> EmitirRemoto([FromBody] EmisionVotoRemotoRequest req)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            req.Emision.Canal = CanalVoto.Remoto;
            var comprobante = _servicio.EmitirRemoto(req.Emision, req.EmailDestino, IpRemoto(), UserAgent());
            return Ok(comprobante);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "EmisionRemoto: payload inválido.");
            return BadRequest(new { mensaje = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "EmisionRemoto: voto rechazado por el método electoral.");
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    private string? IpRemoto() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? UserAgent() =>
        HttpContext.Request.Headers.TryGetValue("User-Agent", out var ua)
            ? ua.ToString()
            : null;
}

public class EmisionVotoRemotoRequest
{
    [Required]
    public EmisionVoto Emision { get; set; } = new();

    [Required]
    [EmailAddress]
    public string EmailDestino { get; set; } = string.Empty;
}
