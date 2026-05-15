using System.Text;
using Election.Core.Models;
using Election.VoteVault.Interfaces;

namespace Election.VoteVault.Services;

// Implementación textual del VVPAT (sin dependencias de PDF).
// El frontend renderiza el texto y permite descargarlo.
// Para producción se reemplaza por GeneradorVvpatPdf basado en QuestPDF.
public class GeneradorVvpatTexto : IGeneradorVvpat
{
    public string GenerarVvpatBase64(ComprobanteVoto comprobante, EmisionVoto emision)
    {
        var sb = new StringBuilder();
        sb.AppendLine("===============================================");
        sb.AppendLine("        COMPROBANTE FISICO VVPAT");
        sb.AppendLine("        Sistema Electoral Colombiano");
        sb.AppendLine("===============================================");
        sb.AppendLine();
        sb.AppendLine($"Numero de confirmacion : {comprobante.NumeroConfirmacion}");
        sb.AppendLine($"Fecha y hora           : {comprobante.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Canal de emision       : {comprobante.Canal}");
        sb.AppendLine($"Circunscripcion        : {emision.CircunscripcionId}");
        sb.AppendLine($"Hash del voto (SHA-256): {comprobante.HashVoto}");
        sb.AppendLine($"ID custodia cifrada    : {comprobante.CustodyId}");
        sb.AppendLine();
        sb.AppendLine("-- Resumen de seleccion (visible al votante) --");
        if (emision.EnBlanco)
        {
            sb.AppendLine("  VOTO EN BLANCO");
        }
        else
        {
            foreach (var pref in emision.Preferencias.OrderBy(p => p.Value))
            {
                sb.AppendLine($"  Preferencia {pref.Value}: {pref.Key}");
            }
        }
        sb.AppendLine();
        sb.AppendLine("-- Firma digital del sistema (RSA-PSS SHA-256) --");
        sb.AppendLine(comprobante.FirmaDigital);
        sb.AppendLine();
        sb.AppendLine("Revise sus selecciones antes de depositar.");
        sb.AppendLine("Este comprobante NO revela el voto al sistema:");
        sb.AppendLine("solo el hash anonimo ha sido registrado.");
        sb.AppendLine("===============================================");

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
