using Election.Core.Models;

namespace Election.Core.Interfaces;

public interface IServicioAsistencia
{
    RespuestaAsistencia RegistrarAsistencia(SolicitudAsistencia solicitud);

    ConteoAsistenciasActa ObtenerConteoActa(string mesaId);

    // Llamado por ServicioEmisionVoto si el voto trae tokenAsistencia.
    // Devuelve el registro asociado, o null si el token es inválido / expirado / ya consumido.
    // Si retorna un registro, lo deja marcado como Consumida.
    RegistroAsistencia? ConsumirToken(string sesionToken);
}
