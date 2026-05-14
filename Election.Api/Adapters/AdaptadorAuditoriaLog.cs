using Election.Core.Interfaces;
using Election.Core.Models;

namespace Election.Api.Adapters;

// Adaptador mock del puerto SR-M6.
// Reemplazar por AdaptadorAuditoriaHttp o AdaptadorAuditoriaLocal
// cuando el equipo de SR-M6 defina la integración real.
public class AdaptadorAuditoriaLog : IPuertoAuditoriaSrM6
{
    private readonly ILogger<AdaptadorAuditoriaLog> _logger;

    public AdaptadorAuditoriaLog(ILogger<AdaptadorAuditoriaLog> logger)
    {
        _logger = logger;
    }

    public void RegistrarEvento(EventoAuditoriaSrM6 evento)
    {
        _logger.LogInformation(
            "[SR-M6] actor={Actor} accion={Accion} objeto={ObjetoTipo}/{ObjetoId} ts={Timestamp:o} ip={Ip} ua={Ua} payload={Payload}",
            evento.Actor, evento.Accion, evento.ObjetoTipo, evento.ObjetoId,
            evento.Timestamp, evento.IpOrigen, evento.UserAgent, evento.PayloadJson);
    }
}
