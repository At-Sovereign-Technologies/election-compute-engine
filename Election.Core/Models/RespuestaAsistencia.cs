namespace Election.Core.Models;

// Devuelto por POST /api/v1/asistencia/registrar.
public class RespuestaAsistencia
{
    public Guid RegistroId { get; set; }
    public string SesionToken { get; set; } = string.Empty;
    public DateTime ExpiraEn { get; set; }
    public TipoAsistencia TipoAsistencia { get; set; }

    // Mensaje libre para mostrar al jurado.
    public string Mensaje { get; set; } = string.Empty;
}
