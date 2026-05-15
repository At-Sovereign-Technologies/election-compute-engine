namespace Election.Core.Interfaces;

// Punto de extensión para validar el handshake del votante presencial (SE-M2).
//
// Hoy el `ValidadorHandshakePermisivo` acepta cualquier handshakeId no vacío.
// Cuando el equipo de SE-M2 levante el servicio de handshake, se reemplaza por
// `ValidadorHandshakeHttp` apuntando a su endpoint, sin tocar ServicioEmisionVoto.
public interface IValidadorHandshake
{
    bool EsValido(string handshakeId);
}
