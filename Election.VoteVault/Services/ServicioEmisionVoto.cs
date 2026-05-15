using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Election.Core.Interfaces;
using Election.Core.Models;
using Election.VoteVault.Interfaces;

namespace Election.VoteVault.Services;

public class ServicioEmisionVoto : IServicioEmisionVoto
{
    private readonly IMetodoElectoral _metodo;
    private readonly IVoteVaultService _vault;
    private readonly IServicioFirmaDigital _firma;
    private readonly IGeneradorVvpat _vvpat;
    private readonly IPuertoAuditoriaSrM6 _auditoria;
    private readonly IPuertoEmailCertificado _email;
    private readonly IValidadorHandshake _handshake;
    private readonly IServicioAsistencia _asistencia;
    private static readonly char[] AlfabetoConfirmacion =
        "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    public ServicioEmisionVoto(
        IMetodoElectoral metodo,
        IVoteVaultService vault,
        IServicioFirmaDigital firma,
        IGeneradorVvpat vvpat,
        IPuertoAuditoriaSrM6 auditoria,
        IPuertoEmailCertificado email,
        IValidadorHandshake handshake,
        IServicioAsistencia asistencia)
    {
        _metodo = metodo;
        _vault = vault;
        _firma = firma;
        _vvpat = vvpat;
        _auditoria = auditoria;
        _email = email;
        _handshake = handshake;
        _asistencia = asistencia;
    }

    public ComprobanteVoto EmitirPresencial(EmisionVoto emision)
    {
        if (string.IsNullOrWhiteSpace(emision.HandshakeId))
        {
            throw new ArgumentException("HandshakeId es obligatorio para emisión presencial.");
        }
        if (!_handshake.EsValido(emision.HandshakeId))
        {
            throw new InvalidOperationException(
                "Handshake inválido o no autorizado para emitir voto presencial.");
        }

        var comprobante = EmitirComun(emision, CanalVoto.Presencial);
        comprobante.VvpatBase64 = _vvpat.GenerarVvpatBase64(comprobante, emision);
        return comprobante;
    }

    public ComprobanteVoto EmitirRemoto(EmisionVoto emision, string emailDestino)
    {
        if (string.IsNullOrWhiteSpace(emailDestino))
        {
            throw new ArgumentException("emailDestino es obligatorio para emisión remota.");
        }

        var comprobante = EmitirComun(emision, CanalVoto.Remoto);
        comprobante.EmailDestino = emailDestino;
        _email.EnviarComprobante(emailDestino, comprobante);
        return comprobante;
    }

    private ComprobanteVoto EmitirComun(EmisionVoto emision, CanalVoto canal)
    {
        var voto = MapearAVoto(emision);

        if (!_metodo.ValidarVoto(voto))
        {
            throw new InvalidOperationException("Voto inválido para el método electoral configurado.");
        }

        // SE-M3-05: si el voto viene con token de asistencia, lo consumo antes
        // de aceptar el voto. Si el token es inválido / expirado / ya consumido,
        // rechazo el voto.
        RegistroAsistencia? asistencia = null;
        if (!string.IsNullOrWhiteSpace(emision.TokenAsistencia))
        {
            asistencia = _asistencia.ConsumirToken(emision.TokenAsistencia);
            if (asistencia is null)
            {
                throw new InvalidOperationException(
                    "Token de asistencia inválido, expirado o ya consumido.");
            }
        }

        string payloadVotoJson = JsonSerializer.Serialize(voto);
        string hashVoto = ComputarSha256Hex(payloadVotoJson);

        // Custodia cifrada con RSA-OAEP-SHA256.
        var custodiado = _vault.CustodyVote(payloadVotoJson);

        // Contabilizar en la urna interna del método electoral.
        _metodo.ContabilizarVoto(voto);

        var timestamp = DateTime.UtcNow;
        var numeroConfirmacion = GenerarNumeroConfirmacion(timestamp);

        var comprobante = new ComprobanteVoto
        {
            CustodyId = custodiado.Id,
            NumeroConfirmacion = numeroConfirmacion,
            HashVoto = hashVoto,
            Timestamp = timestamp,
            Canal = canal,
            VotoAsistido = asistencia is not null,
            RegistroAsistenciaId = asistencia?.Id
        };

        // Firma sobre los campos no mutables.
        comprobante.FirmaDigital = FirmarComprobante(comprobante);

        // Registro en SR-M6 (transparency-service). Política Zero-Identity:
        // solo hashes, IDs criptográficos y metadatos de mesa/elección.
        // Nunca VotanteId, HandshakeId, IP, user-agent ni preferencias.
        var details = new Dictionary<string, object>
        {
            ["custodyId"] = custodiado.Id.ToString(),
            ["voteHash"] = hashVoto,
            ["pollingStationId"] = emision.CircunscripcionId,
            ["cryptographic_protocol"] = "SHA-256",
            ["votoAsistido"] = asistencia is not null
        };
        if (asistencia is not null)
        {
            // Solo el ID del registro de asistencia — NUNCA documento del votante
            // ni del acompañante.
            details["registroAsistenciaId"] = asistencia.Id.ToString();
            details["tipoAsistencia"] = asistencia.TipoAsistencia.ToString();
        }

        _auditoria.RegistrarEvento(new EventoAuditoriaSrM6
        {
            Timestamp = timestamp,
            OriginComponent = "COMPUTE_ENGINE",
            EventType = canal == CanalVoto.Presencial
                ? "VOTE_HASH_REGISTERED_PRESENCIAL"
                : "VOTE_HASH_REGISTERED_REMOTO",
            Severity = "INFO",
            Details = details
        });

        return comprobante;
    }

    private static Voto MapearAVoto(EmisionVoto emision)
    {
        var voto = new Voto
        {
            VotanteId = emision.VotanteId,
            Preferencias = emision.EnBlanco
                ? new Dictionary<string, int> { { "__BLANCO__", 1 } }
                : new Dictionary<string, int>(emision.Preferencias)
        };
        return voto;
    }

    private string FirmarComprobante(ComprobanteVoto c)
    {
        // Concatenación determinística de los campos firmados.
        string payload = string.Join("|",
            c.CustodyId.ToString(),
            c.NumeroConfirmacion,
            c.HashVoto,
            c.Timestamp.ToString("o", CultureInfo.InvariantCulture),
            c.Canal.ToString());
        return _firma.Firmar(payload);
    }

    private static string ComputarSha256Hex(string raw)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes);
    }

    private static string GenerarNumeroConfirmacion(DateTime timestamp)
    {
        // Formato: VC-AAAA-XXXX-YYYY (alfabeto sin caracteres ambiguos).
        Span<byte> aleatorio = stackalloc byte[6];
        RandomNumberGenerator.Fill(aleatorio);

        var bloqueA = new char[4];
        var bloqueB = new char[4];
        for (int i = 0; i < 4; i++)
        {
            bloqueA[i] = AlfabetoConfirmacion[aleatorio[i] % AlfabetoConfirmacion.Length];
        }
        Span<byte> mas = stackalloc byte[4];
        RandomNumberGenerator.Fill(mas);
        for (int i = 0; i < 4; i++)
        {
            bloqueB[i] = AlfabetoConfirmacion[mas[i] % AlfabetoConfirmacion.Length];
        }

        return $"VC-{timestamp.Year}-{new string(bloqueA)}-{new string(bloqueB)}";
    }
}
