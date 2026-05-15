using Election.Core.Models;
using Election.VoteVault.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Election.Api.Adapters;

// Adaptador de correo certificado vía SendGrid.
// Configuración en appsettings:
//   "Email": {
//     "Provider": "SendGrid",
//     "SendGrid": { "ApiKey": "SG.xxx", "FromAddress": "no-reply@selllegitimo.gov.co",
//                   "FromName": "Sello Legítimo" }
//   }
// Activa los siguientes atributos de calidad:
//  - Trazabilidad: SendGrid registra cada email con status y timestamp.
//  - No repudio: webhook de delivery puede ir a SR-M6.
//  - Confiabilidad: retry automático con backoff.
public class AdaptadorEmailSendGrid : IPuertoEmailCertificado
{
    private readonly ISendGridClient _client;
    private readonly string _fromAddress;
    private readonly string _fromName;
    private readonly ILogger<AdaptadorEmailSendGrid> _logger;

    public AdaptadorEmailSendGrid(
        ISendGridClient client,
        IConfiguration cfg,
        ILogger<AdaptadorEmailSendGrid> logger)
    {
        _client = client;
        _fromAddress = cfg["Email:SendGrid:FromAddress"]
            ?? throw new InvalidOperationException("Falta Email:SendGrid:FromAddress en configuración.");
        _fromName = cfg["Email:SendGrid:FromName"] ?? "Sello Legítimo";
        _logger = logger;
    }

    public void EnviarComprobante(string emailDestino, ComprobanteVoto comprobante)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_fromAddress, _fromName),
            Subject = PlantillaCorreoComprobante.ASUNTO,
            PlainTextContent = PlantillaCorreoComprobante.RenderPlainText(comprobante),
            HtmlContent = PlantillaCorreoComprobante.RenderHtml(comprobante)
        };
        msg.AddTo(new EmailAddress(emailDestino));

        // Headers que ayudan a evidenciar no repudio: número de confirmación
        // queda en los headers del email (no extraíble del cuerpo cifrado).
        msg.AddCustomArg("numeroConfirmacion", comprobante.NumeroConfirmacion);
        msg.AddCustomArg("custodyId", comprobante.CustodyId.ToString());

        var response = _client.SendEmailAsync(msg).GetAwaiter().GetResult();

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "[EMAIL-SENDGRID] enviado a {Destino} status={Status} numeroConfirmacion={Numero}",
                emailDestino, (int)response.StatusCode, comprobante.NumeroConfirmacion);
        }
        else
        {
            string body = response.Body.ReadAsStringAsync().GetAwaiter().GetResult();
            _logger.LogError(
                "[EMAIL-SENDGRID] rechazado por SendGrid status={Status} body={Body}",
                (int)response.StatusCode, body);
            throw new InvalidOperationException(
                $"SendGrid devolvió {(int)response.StatusCode}. Verifique API key y from address verificado. Detalle: {body}");
        }
    }
}
