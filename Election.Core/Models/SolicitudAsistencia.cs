using System.ComponentModel.DataAnnotations;

namespace Election.Core.Models;

// Payload de entrada al endpoint POST /api/v1/asistencia/registrar.
// Lo envía el jurado desde la UI antes de iniciar la sesión de voto del votante.
public class SolicitudAsistencia
{
    [Required(ErrorMessage = "El documento del votante es obligatorio.")]
    [MinLength(5, ErrorMessage = "El documento del votante debe tener al menos 5 caracteres.")]
    public string DocumentoVotante { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento del acompañante es obligatorio.")]
    [MinLength(5, ErrorMessage = "El documento del acompañante debe tener al menos 5 caracteres.")]
    public string DocumentoAcompanante { get; set; } = string.Empty;

    [Required]
    public bool EsFamiliar { get; set; }

    [Required(ErrorMessage = "El tipo de asistencia es obligatorio.")]
    public TipoAsistencia TipoAsistencia { get; set; }

    [Required(ErrorMessage = "El id de la mesa es obligatorio.")]
    public string MesaId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El id de la jornada es obligatorio.")]
    public string JornadaId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El id del jurado es obligatorio.")]
    public string JuradoId { get; set; } = string.Empty;
}
