using System.ComponentModel.DataAnnotations;

namespace Election.Core.Models;

public class EmisionVoto
{
    [Required]
    public string VotanteId { get; set; } = string.Empty;

    [Required]
    public CanalVoto Canal { get; set; }

    [Required]
    public string CircunscripcionId { get; set; } = string.Empty;

    // Presencial: token entregado por terminal del jurado tras handshake.
    // Remoto: null.
    public string? HandshakeId { get; set; }

    // Ranking IRV: { "candidatoA": 1, "candidatoB": 2 }.
    // Selección simple: un único entry con valor 1.
    // Voto en blanco: EnBlanco=true y Preferencias vacío.
    public Dictionary<string, int> Preferencias { get; set; } = new();

    public bool EnBlanco { get; set; }

    // SE-M3-05: presente solo cuando es voto asistido. El backend valida la firma
    // del token (RSA-PSS), su vigencia, y que no haya sido consumido previamente.
    // El registro de asistencia queda vinculado al voto por referencia (registroId),
    // nunca por documento.
    public string? TokenAsistencia { get; set; }
}
