namespace Election.Core.Models;

public class Acta
{
    public List<string> Eventos { get; set; } = new();

    public int VotosValidos    { get; set; }
    public int VotosEnBlanco   { get; set; }
    public int VotosNoMarcados { get; set; }
    public int VotosNulos      { get; set; }

    public int TotalVotos => VotosValidos + VotosEnBlanco + VotosNoMarcados + VotosNulos;

    public void RegistrarVoto(TipoVoto tipo)
    {
        switch (tipo)
        {
            case TipoVoto.Valido:     VotosValidos++;     break;
            case TipoVoto.EnBlanco:   VotosEnBlanco++;    break;
            case TipoVoto.NoMarcado:  VotosNoMarcados++;  break;
            case TipoVoto.Nulo:       VotosNulos++;       break;
        }
        Eventos.Add($"[{DateTime.UtcNow:O}] Voto registrado: {tipo}");
    }
}