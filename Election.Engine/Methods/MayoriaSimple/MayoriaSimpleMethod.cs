using Election.Core.Interfaces;
using Election.Core.Models;

namespace Election.Engine.Methods.MayoriaSimple;

public class MayoriaSimpleMethod : IMetodoElectoral
{
    private readonly List<Voto> _urna = new();
    private readonly Acta _acta = new();
    private readonly List<string> _candidatosValidos;

    // SR-M6: semilla auditable para desempate
    private string? _semillaDesempate;

    public MayoriaSimpleMethod(List<string> candidatosValidos)
    {
        _candidatosValidos = candidatosValidos;
    }

    public bool ValidarVoto(Voto voto)
    {
        voto.Tipo = Voto.Clasificar(voto, _candidatosValidos);

        // US-01: votos nulos se registran pero no se cuentan en el escrutinio
        return voto.Tipo != TipoVoto.Nulo;
    }

    public void ContabilizarVoto(Voto voto)
    {
        _acta.RegistrarVoto(voto.Tipo);
        _urna.Add(voto);
    }

    public Resultado CalcularResultado()
    {
        // US-03: sumar votos válidos por candidato
        var conteo = _candidatosValidos.ToDictionary(c => c, _ => 0m);

        foreach (var voto in _urna.Where(v => v.Tipo == TipoVoto.Valido))
        {
            var candidato = voto.Preferencias
                .OrderBy(p => p.Value)
                .First().Key;

            if (conteo.ContainsKey(candidato))
                conteo[candidato]++;
        }

        // US-03: ordenar de mayor a menor
        var ordenado = conteo.OrderByDescending(x => x.Value).ToList();
        var primero  = ordenado[0];
        var segundo  = ordenado.Count > 1 ? ordenado[1] : default;

        var resultado = new Resultado { Totales = conteo };

        // US-03: empate técnico → SR-M6
        if (segundo.Key != null && primero.Value == segundo.Value)
        {
            _semillaDesempate = Guid.NewGuid().ToString("N");
            _acta.Eventos.Add($"[EMPATE] Semilla SR-M6: {_semillaDesempate}");

            resultado.Ganador    = $"__EMPATE__:{primero.Key}:{segundo.Key}";
            resultado.Porcentaje = 0;
            return resultado;
        }

        decimal total = conteo.Values.Sum();
        resultado.Ganador    = primero.Key;
        resultado.Porcentaje = total > 0 ? primero.Value / total : 0;
        return resultado;
    }

    public bool RequiereSegundaVuelta() => false;

    public Acta GenerarActa() => _acta;
}