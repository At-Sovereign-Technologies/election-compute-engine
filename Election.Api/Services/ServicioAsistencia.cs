using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Election.Api.Data;
using Election.Core.Interfaces;
using Election.Core.Models;
using Election.VoteVault.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Election.Api.Services;

// SE-M3-05 — Gestión de voto asistido.
// Lógica:
//  1. Hashea los documentos (SHA-256). Nunca persiste en claro.
//  2. Si el acompañante NO es familiar, valida que no haya asistido a otro
//     votante NO familiar en la misma jornada. Si excede, lanza ExcedeLimiteAcompananteException.
//  3. Persiste el registro en BD local.
//  4. Emite token de sesión asistida firmado RSA-PSS (válido 30 min).
//  5. Registra evento en SR-M6 con HASHES (no documentos), cumpliendo Zero-Identity.
public class ServicioAsistencia : IServicioAsistencia
{
    public const int LIMITE_NO_FAMILIAR_POR_JORNADA = 1;
    public static readonly TimeSpan VIGENCIA_TOKEN = TimeSpan.FromMinutes(30);

    private readonly IDbContextFactory<AsistenciaDbContext> _dbFactory;
    private readonly IServicioFirmaDigital _firma;
    private readonly IPuertoAuditoriaSrM6 _auditoria;
    private readonly ILogger<ServicioAsistencia> _logger;

    public ServicioAsistencia(
        IDbContextFactory<AsistenciaDbContext> dbFactory,
        IServicioFirmaDigital firma,
        IPuertoAuditoriaSrM6 auditoria,
        ILogger<ServicioAsistencia> logger)
    {
        _dbFactory = dbFactory;
        _firma = firma;
        _auditoria = auditoria;
        _logger = logger;
    }

    public RespuestaAsistencia RegistrarAsistencia(SolicitudAsistencia s)
    {
        string hashVotante = HashSha256(s.DocumentoVotante);
        string hashAcompanante = HashSha256(s.DocumentoAcompanante);

        using var db = _dbFactory.CreateDbContext();

        // CA #3: validar que el acompañante no haya asistido a más de un
        // votante NO familiar en la jornada.
        if (!s.EsFamiliar)
        {
            int asistenciasNoFamiliares = db.Registros.Count(r =>
                r.HashDocAcompanante == hashAcompanante &&
                r.JornadaId == s.JornadaId &&
                !r.EsFamiliar &&
                r.Estado != "Bloqueada");

            if (asistenciasNoFamiliares >= LIMITE_NO_FAMILIAR_POR_JORNADA)
            {
                // CA #4: bloquear y notificar.
                RegistrarBloqueoEnSrM6(hashAcompanante, s, asistenciasNoFamiliares);
                throw new ExcedeLimiteAcompananteException(
                    $"El acompañante ya asistió a {asistenciasNoFamiliares} votante(s) no familiar(es) en esta jornada. " +
                    $"Límite permitido: {LIMITE_NO_FAMILIAR_POR_JORNADA}.");
            }
        }

        var registro = new RegistroAsistencia
        {
            HashDocVotante = hashVotante,
            HashDocAcompanante = hashAcompanante,
            EsFamiliar = s.EsFamiliar,
            TipoAsistencia = s.TipoAsistencia,
            MesaId = s.MesaId,
            JornadaId = s.JornadaId,
            JuradoId = s.JuradoId,
            Timestamp = DateTime.UtcNow,
            Estado = "Activa"
        };

        registro.SesionToken = GenerarTokenFirmado(registro);

        db.Registros.Add(registro);
        db.SaveChanges();

        // CA #2: registrar en SR-M6 con hashes (Zero-Identity).
        _auditoria.RegistrarEvento(new EventoAuditoriaSrM6
        {
            Timestamp = registro.Timestamp,
            OriginComponent = "COMPUTE_ENGINE",
            EventType = "ASISTENCIA_REGISTRADA",
            Severity = "INFO",
            Details = new Dictionary<string, object>
            {
                ["registroId"] = registro.Id.ToString(),
                ["hashDocVotante"] = hashVotante,
                ["hashDocAcompanante"] = hashAcompanante,
                ["esFamiliar"] = s.EsFamiliar,
                ["tipoAsistencia"] = s.TipoAsistencia.ToString(),
                ["mesaId"] = s.MesaId,
                ["jornadaId"] = s.JornadaId,
                ["juradoId"] = s.JuradoId,
                ["cryptographic_protocol"] = "SHA-256"
            }
        });

        _logger.LogInformation(
            "[ASISTENCIA] registrada mesa={Mesa} jornada={Jornada} familiar={Familiar} tipo={Tipo}",
            s.MesaId, s.JornadaId, s.EsFamiliar, s.TipoAsistencia);

        return new RespuestaAsistencia
        {
            RegistroId = registro.Id,
            SesionToken = registro.SesionToken,
            ExpiraEn = registro.Timestamp.Add(VIGENCIA_TOKEN),
            TipoAsistencia = registro.TipoAsistencia,
            Mensaje = "Sesión asistida registrada. El votante ya puede ingresar al tarjetón."
        };
    }

    public ConteoAsistenciasActa ObtenerConteoActa(string mesaId)
    {
        using var db = _dbFactory.CreateDbContext();
        var rows = db.Registros
            .Where(r => r.MesaId == mesaId && r.Estado == "Consumida")
            .ToList();

        var porTipo = rows
            .GroupBy(r => r.TipoAsistencia)
            .ToDictionary(g => g.Key, g => g.Count());

        return new ConteoAsistenciasActa
        {
            MesaId = mesaId,
            TotalAsistidos = rows.Count,
            PorTipo = porTipo,
            TotalFamiliares = rows.Count(r => r.EsFamiliar),
            TotalNoFamiliares = rows.Count(r => !r.EsFamiliar)
        };
    }

    public RegistroAsistencia? ConsumirToken(string sesionToken)
    {
        if (string.IsNullOrWhiteSpace(sesionToken)) return null;

        using var db = _dbFactory.CreateDbContext();
        var registro = db.Registros.FirstOrDefault(r => r.SesionToken == sesionToken);

        if (registro == null) return null;
        if (registro.Estado != "Activa") return null;
        if (DateTime.UtcNow > registro.Timestamp.Add(VIGENCIA_TOKEN)) return null;

        // Verificar firma.
        string payloadFirmado = ConstruirPayloadFirma(registro);
        // El token ES la firma, así que verificamos firma == token sobre payload.
        if (!_firma.Verificar(payloadFirmado, sesionToken)) return null;

        registro.Estado = "Consumida";
        db.SaveChanges();
        return registro;
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private string GenerarTokenFirmado(RegistroAsistencia r)
    {
        // El token es la firma RSA-PSS del payload determinístico.
        // ServicioFirmaDigital ya está en VoteVault con la clave del sistema.
        return _firma.Firmar(ConstruirPayloadFirma(r));
    }

    private static string ConstruirPayloadFirma(RegistroAsistencia r)
    {
        return string.Join("|",
            r.Id.ToString(),
            r.HashDocVotante,
            r.HashDocAcompanante,
            r.MesaId,
            r.JornadaId,
            r.Timestamp.ToString("o", CultureInfo.InvariantCulture));
    }

    private void RegistrarBloqueoEnSrM6(
        string hashAcompanante,
        SolicitudAsistencia s,
        int asistenciasPrevias)
    {
        try
        {
            _auditoria.RegistrarEvento(new EventoAuditoriaSrM6
            {
                Timestamp = DateTime.UtcNow,
                OriginComponent = "COMPUTE_ENGINE",
                EventType = "ASISTENCIA_BLOQUEADA_LIMITE",
                Severity = "WARNING",
                Details = new Dictionary<string, object>
                {
                    ["hashDocAcompanante"] = hashAcompanante,
                    ["asistenciasPreviasNoFamiliares"] = asistenciasPrevias,
                    ["limite"] = LIMITE_NO_FAMILIAR_POR_JORNADA,
                    ["mesaId"] = s.MesaId,
                    ["jornadaId"] = s.JornadaId,
                    ["juradoId"] = s.JuradoId,
                    ["cryptographic_protocol"] = "SHA-256"
                }
            });
        }
        catch (Exception ex)
        {
            // No queremos que un fallo de auditoría tape el error de negocio
            // que ya estamos a punto de lanzar al jurado.
            _logger.LogWarning(ex, "[ASISTENCIA] no se pudo registrar el bloqueo en SR-M6.");
        }
    }

    private static string HashSha256(string raw)
    {
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw.Trim().ToUpperInvariant()));
        return Convert.ToHexString(bytes);
    }
}

public class ExcedeLimiteAcompananteException : Exception
{
    public ExcedeLimiteAcompananteException(string mensaje) : base(mensaje) {}
}
