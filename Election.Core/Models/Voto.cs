namespace Election.Core.Models;

public enum TipoVoto
{
    Valido,
    EnBlanco,
    NoMarcado,
    Nulo
}

public class Voto
{
    public string VotanteId { get; set; } = string.Empty;
    public Dictionary<string, int> Preferencias { get; set; } = new();

    public TipoVoto Tipo { get; set; } = TipoVoto.Valido;

    public static TipoVoto Clasificar(Voto voto, IEnumerable<string> candidatosValidos)
    {
        if (voto.Preferencias.ContainsKey("__BLANCO__"))
            return TipoVoto.EnBlanco;

        if (!voto.Preferencias.Any())
            return TipoVoto.NoMarcado;

        bool tieneOpcionValida = voto.Preferencias.Keys
            .Any(k => candidatosValidos.Contains(k));

        return tieneOpcionValida ? TipoVoto.Valido : TipoVoto.Nulo;
    }
}