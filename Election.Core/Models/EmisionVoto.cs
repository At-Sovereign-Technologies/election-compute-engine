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
}
