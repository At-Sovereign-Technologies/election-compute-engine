namespace Election.Core.Models;

public class ComprobanteVoto
{
    public Guid CustodyId { get; set; }

    // Legible para el votante: "VC-2026-A8X4-K2P9".
    public string NumeroConfirmacion { get; set; } = string.Empty;

    // SHA-256 del voto en claro; el contenido NO viaja, solo el hash.
    public string HashVoto { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public CanalVoto Canal { get; set; }

    // RSA sign del comprobante completo (sin VVPAT/email).
    public string FirmaDigital { get; set; } = string.Empty;

    // Presencial: PDF base64 con QR del hash. Remoto: null.
    public string? VvpatBase64 { get; set; }

    // Remoto: correo certificado destino. Presencial: null.
    public string? EmailDestino { get; set; }
}
