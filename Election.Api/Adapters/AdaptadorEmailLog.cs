using Election.Core.Models;
using Election.VoteVault.Interfaces;

namespace Election.Api.Adapters;

// Adaptador mock del puerto de email certificado.
// Reemplazar por AdaptadorEmailSmtp / AdaptadorEmailGraph cuando el equipo
// defina el proveedor real (Postfix, SendGrid, Graph API, etc.).
public class AdaptadorEmailLog : IPuertoEmailCertificado
{
    private readonly ILogger<AdaptadorEmailLog> _logger;

    public AdaptadorEmailLog(ILogger<AdaptadorEmailLog> logger)
    {
        _logger = logger;
    }

    public void EnviarComprobante(string emailDestino, ComprobanteVoto comprobante)
    {
        _logger.LogInformation(
            "[EMAIL-CERTIFICADO] to={Destino} numeroConfirmacion={Numero} hashVoto={Hash} ts={Timestamp:o} firma={FirmaLen} chars",
            emailDestino,
            comprobante.NumeroConfirmacion,
            comprobante.HashVoto,
            comprobante.Timestamp,
            comprobante.FirmaDigital.Length);
    }
}
