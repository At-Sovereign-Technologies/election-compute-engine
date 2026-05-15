using System.Net;
using System.Net.Mail;
using Election.Core.Models;
using Election.VoteVault.Interfaces;

namespace Election.Api.Adapters;

// Adaptador de correo certificado vía SMTP genérico.
// Sirve para:
//   - MailHog en desarrollo (docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog)
//   - Postfix o cualquier servidor SMTP en producción si no se usa SendGrid
//
// Configuración en appsettings:
//   "Email": {
//     "Provider": "Smtp",
//     "Smtp": {
//       "Host": "localhost",
//       "Port": 1025,
//       "EnableSsl": false,
//       "User": "",
//       "Password": "",
//       "FromAddress": "no-reply@sellolegitimo.local"
//     }
//   }
public class AdaptadorEmailSmtp : IPuertoEmailCertificado
{
    private readonly string _host;
    private readonly int _port;
    private readonly bool _enableSsl;
    private readonly string? _user;
    private readonly string? _password;
    private readonly string _fromAddress;
    private readonly ILogger<AdaptadorEmailSmtp> _logger;

    public AdaptadorEmailSmtp(IConfiguration cfg, ILogger<AdaptadorEmailSmtp> logger)
    {
        _host = cfg["Email:Smtp:Host"]
            ?? throw new InvalidOperationException("Falta Email:Smtp:Host en configuración.");
        _port = int.Parse(cfg["Email:Smtp:Port"] ?? "25");
        _enableSsl = bool.Parse(cfg["Email:Smtp:EnableSsl"] ?? "false");
        _user = cfg["Email:Smtp:User"];
        _password = cfg["Email:Smtp:Password"];
        _fromAddress = cfg["Email:Smtp:FromAddress"]
            ?? throw new InvalidOperationException("Falta Email:Smtp:FromAddress en configuración.");
        _logger = logger;
    }

    public void EnviarComprobante(string emailDestino, ComprobanteVoto comprobante)
    {
        using var client = new SmtpClient(_host, _port)
        {
            EnableSsl = _enableSsl
        };

        if (!string.IsNullOrEmpty(_user))
        {
            client.Credentials = new NetworkCredential(_user, _password);
        }

        using var msg = new MailMessage
        {
            From = new MailAddress(_fromAddress, "Sello Legítimo"),
            Subject = PlantillaCorreoComprobante.ASUNTO,
            Body = PlantillaCorreoComprobante.RenderHtml(comprobante),
            IsBodyHtml = true,
        };
        msg.To.Add(emailDestino);
        msg.Headers.Add("X-Sello-Legitimo-Numero-Confirmacion", comprobante.NumeroConfirmacion);
        msg.Headers.Add("X-Sello-Legitimo-Custody-Id", comprobante.CustodyId.ToString());

        try
        {
            client.Send(msg);
            _logger.LogInformation(
                "[EMAIL-SMTP] enviado a {Destino} via {Host}:{Port} numeroConfirmacion={Numero}",
                emailDestino, _host, _port, comprobante.NumeroConfirmacion);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex,
                "[EMAIL-SMTP] fallo enviando a {Destino} via {Host}:{Port}",
                emailDestino, _host, _port);
            throw new InvalidOperationException(
                $"No fue posible enviar el comprobante por SMTP a {emailDestino}.", ex);
        }
    }
}
