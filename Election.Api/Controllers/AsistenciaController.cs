using Election.Api.Services;
using Election.Core.Interfaces;
using Election.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace Election.Api.Controllers;

[ApiController]
[Route("api/v1/asistencia")]
public class AsistenciaController : ControllerBase
{
    private readonly IServicioAsistencia _servicio;
    private readonly ILogger<AsistenciaController> _logger;

    public AsistenciaController(
        IServicioAsistencia servicio,
        ILogger<AsistenciaController> logger)
    {
        _servicio = servicio;
        _logger = logger;
    }

    // SE-M3-05 CA #1-#4: registrar la asistencia antes de iniciar la sesión.
    [HttpPost("registrar")]
    public ActionResult<RespuestaAsistencia> Registrar([FromBody] SolicitudAsistencia solicitud)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var respuesta = _servicio.RegistrarAsistencia(solicitud);
            return Ok(respuesta);
        }
        catch (ExcedeLimiteAcompananteException ex)
        {
            // CA #4: bloquear y notificar al jurado.
            _logger.LogWarning("[ASISTENCIA] bloqueo por exceso de límite: {Mensaje}", ex.Message);
            return StatusCode(StatusCodes.Status409Conflict, new
            {
                codigo = "EXCEDE_LIMITE_ACOMPANANTE",
                mensaje = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { mensaje = ex.Message });
        }
    }

    // SE-M3-05 CA #5: acta de mesa con conteo agregado SIN identidades.
    [HttpGet("acta/{mesaId}")]
    public ActionResult<ConteoAsistenciasActa> ObtenerActa(string mesaId)
    {
        if (string.IsNullOrWhiteSpace(mesaId))
        {
            return BadRequest(new { mensaje = "mesaId es obligatorio." });
        }
        return Ok(_servicio.ObtenerConteoActa(mesaId));
    }
}
