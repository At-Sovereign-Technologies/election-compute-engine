using System.Net;
using System.Text;
using Election.Core.Models;

namespace Election.Api.Adapters;

// Renderiza el comprobante de voto remoto como HTML para email certificado.
// Compartido por todos los adaptadores de IPuertoEmailCertificado.
public static class PlantillaCorreoComprobante
{
    public static string RenderHtml(ComprobanteVoto c)
    {
        string firmaCorta = c.FirmaDigital.Length > 64
            ? c.FirmaDigital[..64] + "..."
            : c.FirmaDigital;

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><body style=\"font-family:-apple-system,Segoe UI,Roboto,sans-serif;background:#f5f6f7;margin:0;padding:24px;color:#1f2937\">");
        sb.AppendLine("  <div style=\"max-width:560px;margin:0 auto;background:#fff;border:1px solid #e5e7eb;border-radius:16px;padding:32px\">");
        sb.AppendLine("    <div style=\"display:flex;align-items:center;gap:12px;margin-bottom:24px\">");
        sb.AppendLine("      <div style=\"width:40px;height:40px;background:#ef4444;border-radius:8px;display:flex;align-items:center;justify-content:center;color:#fff;font-weight:bold\">✓</div>");
        sb.AppendLine("      <div><h1 style=\"margin:0;font-size:18px;font-weight:800\">Sello Legítimo</h1>");
        sb.AppendLine("      <p style=\"margin:0;font-size:11px;color:#ef4444;font-weight:600;letter-spacing:.05em;text-transform:uppercase\">Sistema Electoral Colombiano</p></div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <h2 style=\"font-size:22px;font-weight:800;margin:0 0 8px\">Voto registrado y custodiado</h2>");
        sb.AppendLine("    <p style=\"color:#6b7280;font-size:14px;margin:0 0 24px\">Conserve este comprobante. El sistema solo almacenó el hash anónimo de su voto.</p>");
        sb.AppendLine("    <div style=\"border-top:1px solid #e5e7eb;border-bottom:1px solid #e5e7eb;padding:20px 0;margin-bottom:20px\">");
        sb.AppendLine("      <p style=\"text-transform:uppercase;font-size:11px;font-weight:600;color:#9ca3af;letter-spacing:.1em;margin:0 0 8px\">Número de confirmación</p>");
        sb.AppendLine($"      <p style=\"font-family:'Courier New',monospace;font-size:28px;font-weight:800;color:#ef4444;margin:0;letter-spacing:.15em\">{WebUtility.HtmlEncode(c.NumeroConfirmacion)}</p>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <table style=\"width:100%;font-size:13px\" cellpadding=\"6\">");
        sb.AppendLine($"      <tr><td style=\"color:#6b7280;width:180px\">Canal</td><td><b>{c.Canal}</b></td></tr>");
        sb.AppendLine($"      <tr><td style=\"color:#6b7280\">Timestamp</td><td style=\"font-family:monospace;font-size:12px\">{c.Timestamp:o}</td></tr>");
        sb.AppendLine($"      <tr><td style=\"color:#6b7280\">Hash del voto (SHA-256)</td><td style=\"font-family:monospace;font-size:11px;word-break:break-all\">{WebUtility.HtmlEncode(c.HashVoto)}</td></tr>");
        sb.AppendLine($"      <tr><td style=\"color:#6b7280\">ID custodia cifrada</td><td style=\"font-family:monospace;font-size:11px;word-break:break-all\">{c.CustodyId}</td></tr>");
        sb.AppendLine($"      <tr><td style=\"color:#6b7280;vertical-align:top\">Firma digital (RSA-PSS)</td><td style=\"font-family:monospace;font-size:10px;word-break:break-all;color:#6b7280\">{WebUtility.HtmlEncode(firmaCorta)}</td></tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("    <p style=\"font-size:11px;color:#9ca3af;margin-top:24px;line-height:1.5\">Este correo es un comprobante criptográficamente firmado. La integridad puede verificarse contra la clave pública del sistema. El contenido de su voto nunca se transmite por correo: solo el hash anónimo ha sido registrado en el log de auditoría inmutable.</p>");
        sb.AppendLine("  </div>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    public static string RenderPlainText(ComprobanteVoto c)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SELLO LEGITIMO - Comprobante de voto");
        sb.AppendLine("=====================================");
        sb.AppendLine($"Numero confirmacion : {c.NumeroConfirmacion}");
        sb.AppendLine($"Canal               : {c.Canal}");
        sb.AppendLine($"Timestamp           : {c.Timestamp:o}");
        sb.AppendLine($"Hash voto (SHA-256) : {c.HashVoto}");
        sb.AppendLine($"ID custodia         : {c.CustodyId}");
        sb.AppendLine();
        sb.AppendLine("Firma digital (RSA-PSS):");
        sb.AppendLine(c.FirmaDigital);
        return sb.ToString();
    }

    public const string ASUNTO = "Su comprobante de voto - Sello Legítimo";
}
