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
        var resultado = new Resultado();

        if (!_urna.Any())
        {
            resultado.Ganador = "Sin votos";
            resultado.Porcentaje = 0;

            return resultado;
        }

        var candidatos = _urna
            .SelectMany(v => v.Preferencias.Keys)
            .Distinct()
            .ToList();

        var eliminados = new HashSet<string>();

        int ronda = 1;

        while (true)
        {
            var conteo = candidatos
                .Where(c => !eliminados.Contains(c))
                .ToDictionary(c => c, c => 0m);

            decimal votosAgotados = 0;

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
                else
                {
                    votosAgotados++;
                }
            }

            decimal totalActivos = conteo.Values.Sum();

            foreach (var item in conteo)
            {
                decimal porcentaje = item.Value / totalActivos;

                if (porcentaje > 0.5m)
                {
                    resultado.Ganador = item.Key;
                    resultado.Porcentaje = porcentaje;
                    resultado.Totales = conteo;

                    resultado.Rondas.Add(new RondaResultado
                    {
                        NumeroRonda = ronda,
                        Conteo = new Dictionary<string, decimal>(conteo),
                        VotosAgotados = votosAgotados
                    });

                    return resultado;
                }
            }

            var eliminado = conteo
                .OrderBy(x => x.Value)
                .First();

            resultado.Rondas.Add(new RondaResultado
            {
                NumeroRonda = ronda,
                Conteo = new Dictionary<string, decimal>(conteo),
                CandidatoEliminado = eliminado.Key,
                VotosAgotados = votosAgotados
            });

            eliminados.Add(eliminado.Key);

            ronda++;
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