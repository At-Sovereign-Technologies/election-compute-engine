namespace Election.Core.Models;

// Persistido en la BD del election-compute-engine.
// Política: NUNCA se guarda el documento en claro.
// El hash sirve para detectar duplicados sin exponer identidad.
public class RegistroAsistencia
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // SHA-256 del documento del votante asistido (para auditoría).
    public string HashDocVotante { get; set; } = string.Empty;

    // SHA-256 del documento del acompañante (clave de validación de duplicados).
    public string HashDocAcompanante { get; set; } = string.Empty;

    // El jurado declara si el acompañante es familiar del votante.
    // Si es familiar, no aplica el límite "1 acompañante no-familiar por jornada".
    public bool EsFamiliar { get; set; }

    public TipoAsistencia TipoAsistencia { get; set; }

    public string MesaId { get; set; } = string.Empty;

    public string JornadaId { get; set; } = string.Empty;

    public string JuradoId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Token de sesión asistida firmado RSA-PSS, válido por 30 min.
    // El frontend lo pasa al endpoint de emisión de voto, y el backend lo
    // valida + marca como consumido para evitar replay.
    public string SesionToken { get; set; } = string.Empty;

    // Estados:
    //   Activa       — token emitido, sin consumir.
    //   Consumida    — el voto ya se emitió usando este token.
    //   Bloqueada    — registro rechazado (acompañante excede límite, etc.).
    public string Estado { get; set; } = "Activa";
}
