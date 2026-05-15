using Election.Core.Models;

namespace Election.Core.Interfaces;

public interface IServicioEmisionVoto
{
    ComprobanteVoto EmitirPresencial(EmisionVoto emision);

    ComprobanteVoto EmitirRemoto(EmisionVoto emision, string emailDestino);
}
