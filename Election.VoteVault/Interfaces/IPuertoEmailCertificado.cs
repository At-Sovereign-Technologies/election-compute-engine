using Election.Core.Models;

namespace Election.VoteVault.Interfaces;

public interface IPuertoEmailCertificado
{
    // Envía el comprobante de voto al correo certificado del ciudadano.
    // SE-M3-02: "Enviar documento de confirmación al correo certificado del ciudadano".
    void EnviarComprobante(string emailDestino, ComprobanteVoto comprobante);
}
