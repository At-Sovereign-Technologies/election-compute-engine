using System.Linq;
using Election.Core.Interfaces;
using Election.Core.Models;

namespace Election.Engine.Methods.AlternativeVote;

public class AlternativeVoteMethod : IMetodoElectoral
{
    private readonly List<Voto> _urna = new();

    public bool ValidarVoto(Voto voto)
    {
        var rankings = voto.Preferencias.Values;

        return rankings.Distinct().Count() == rankings.Count();
    }

    public void ContabilizarVoto(Voto voto)
    {
        _urna.Add(voto);
    }

    public Resultado CalcularResultado()
    {
        if (!_urna.Any())
        {
            return new Resultado
            {
                Ganador = "Sin votos",
                Porcentaje = 0,
                Totales = new Dictionary<string, decimal>()
            };
        }

        var candidatos = _urna
            .SelectMany(v => v.Preferencias.Keys)
            .Distinct()
            .ToList();

        var eliminados = new HashSet<string>();

        while (true)
        {
            var conteo = candidatos
                .Where(c => !eliminados.Contains(c))
                .ToDictionary(c => c, c => 0m);

            if (!conteo.Any())
            {
                return new Resultado
                {
                    Ganador = "Sin ganador",
                    Porcentaje = 0,
                    Totales = new Dictionary<string, decimal>()
                };
            }

            foreach (var voto in _urna)
            {
                var preferenciaActiva = voto.Preferencias
                    .Where(p => !eliminados.Contains(p.Key))
                    .OrderBy(p => p.Value)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(preferenciaActiva.Key))
                {
                    conteo[preferenciaActiva.Key]++;
                }
            }

            decimal totalActivos = conteo.Values.Sum();

            foreach (var item in conteo)
            {
                decimal porcentaje = item.Value / totalActivos;

                if (porcentaje > 0.5m)
                {
                    return new Resultado
                    {
                        Ganador = item.Key,
                        Porcentaje = porcentaje,
                        Totales = conteo
                    };
                }
            }

            var eliminado = conteo
                .OrderBy(x => x.Value)
                .First();

            eliminados.Add(eliminado.Key);
        }
    }

    public bool RequiereSegundaVuelta()
    {
        return false;
    }

    public Acta GenerarActa()
    {
        return new Acta
        {
            Eventos = new List<string>
            {
                "Acta generada para método AV"
            }
        };
    }
}