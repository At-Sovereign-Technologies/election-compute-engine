using Election.Core.Models;

namespace Election.VoteVault.Interfaces;

public interface IGeneradorVvpat
{
    // Genera un comprobante físico (PDF base64) con QR del hash, número de confirmación
    // y resumen visible al votante para revisión antes del depósito.
    string GenerarVvpatBase64(ComprobanteVoto comprobante, EmisionVoto emision);
}
