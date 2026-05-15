using Election.Core.Interfaces;
using Election.Core.Models;

namespace Election.Engine.Methods.CifraRepartidora;

public record Lista(string Id, List<string> CandidatosOrdenados, bool EsCerrada);

public class CifraRepartidoraMethod : IMetodoElectoral
{
    private readonly List<Voto> _urna = new();
    private readonly Acta _acta = new();
    private readonly List<Lista> _listas;
    private readonly int _curules;
    private readonly decimal _umbralPorcentaje; // ej. 0.03 para 3%

    // votos preferentes individuales: candidato → conteo
    private readonly Dictionary<string, int> _votosPreferentes = new();

    public CifraRepartidoraMethod(List<Lista> listas, int curules, decimal umbralPorcentaje = 0.03m)
    {
        _listas             = listas;
        _curules            = curules;
        _umbralPorcentaje   = umbralPorcentaje;
    }

    public bool ValidarVoto(Voto voto)
    {
        // cada voto tiene exactamente una lista como clave principal
        return voto.Preferencias.Any();
    }

    public void ContabilizarVoto(Voto voto)
    {
        _acta.RegistrarVoto(TipoVoto.Valido);
        _urna.Add(voto);

        // acumular votos preferentes individuales (para listas abiertas)
        foreach (var (candidato, _) in voto.Preferencias.Where(p => p.Key.Contains(':')))
            _votosPreferentes[candidato] = _votosPreferentes.GetValueOrDefault(candidato) + 1;
    }

    public Resultado CalcularResultado()
    {
        // votos por lista: clave es el ID de la lista
        var votosPorLista = _listas.ToDictionary(l => l.Id, _ => 0m);

        foreach (var voto in _urna)
        {
            var listaId = voto.Preferencias.Keys.FirstOrDefault(k => !k.Contains(':'));
            if (listaId != null && votosPorLista.ContainsKey(listaId))
                votosPorLista[listaId]++;
        }

        decimal totalValidos = votosPorLista.Values.Sum();

        // US-05 Fase 1: filtrar listas que no superen el umbral
        decimal umbralVotos = totalValidos * _umbralPorcentaje;
        var listasSobrevivientes = _listas
            .Where(l => votosPorLista[l.Id] >= umbralVotos)
            .ToList();

        _acta.Eventos.Add($"[FASE 1] Listas sobrevivientes: {string.Join(", ", listasSobrevivientes.Select(l => l.Id))}");

        // US-05 Fase 2: D'Hondt — matriz de cocientes
        var cocientes = new List<(string ListaId, int Divisor, decimal Cociente)>();

        foreach (var lista in listasSobrevivientes)
            for (int d = 1; d <= _curules; d++)
                cocientes.Add((lista.Id, d, votosPorLista[lista.Id] / d));

        var asignaciones = cocientes
            .OrderByDescending(c => c.Cociente)
            .Take(_curules)
            .GroupBy(c => c.ListaId)
            .ToDictionary(g => g.Key, g => g.Count());

        _acta.Eventos.Add($"[FASE 2] Curules por lista: {string.Join(", ", asignaciones.Select(a => $"{a.Key}={a.Value}"))}");

        // US-05 Fase 3: asignación interna por lista
        var asignacionFinal = new Dictionary<string, decimal>();

        foreach (var lista in listasSobrevivientes)
        {
            if (!asignaciones.TryGetValue(lista.Id, out int curulesLista)) continue;

            IEnumerable<string> candidatosAsignados;

            if (lista.EsCerrada)
            {
                // lista cerrada: orden de inscripción
                candidatosAsignados = lista.CandidatosOrdenados.Take(curulesLista);
            }
            else
            {
                // lista abierta: orden por votos preferentes individuales
                candidatosAsignados = lista.CandidatosOrdenados
                    .OrderByDescending(c => _votosPreferentes.GetValueOrDefault($"{lista.Id}:{c}"))
                    .Take(curulesLista);
            }

            foreach (var candidato in candidatosAsignados)
                asignacionFinal[$"{lista.Id}:{candidato}"] = curulesLista;

            _acta.Eventos.Add($"[FASE 3] {lista.Id} ({(lista.EsCerrada ? "cerrada" : "abierta")}): {string.Join(", ", candidatosAsignados)}");
        }

        return new Resultado
        {
            Ganador  = "Ver Totales para distribución de curules",
            Totales  = asignacionFinal
        };
    }

    public bool RequiereSegundaVuelta() => false;

    public Acta GenerarActa() => _acta;
}