using Election.Core.Models;

namespace Election.Core.Interfaces;

public interface IServicioEmisionVoto
{
    ComprobanteVoto EmitirPresencial(EmisionVoto emision, string? ipOrigen, string? userAgent);

    ComprobanteVoto EmitirRemoto(EmisionVoto emision, string emailDestino, string? ipOrigen, string? userAgent);
}
