using Election.Core.Interfaces;
using Election.Core.Models;

namespace Election.Engine.Methods.MayoriaAbsoluta;

public class MayoriaAbsolutaMethod : IMetodoElectoral
{
    private readonly List<Voto> _urna = new();
    private readonly Acta _acta = new();
    private readonly List<string> _candidatosValidos;
    private readonly decimal _umbral;

    // US-04: flags de resultado
    public bool RepeticionEleccion    { get; private set; }
    public bool RequiereSegundaVuelta { get; private set; }
    public List<string> FinalistasSegundaVuelta { get; private set; } = new();

    bool IMetodoElectoral.RequiereSegundaVuelta() => RequiereSegundaVuelta;

    public MayoriaAbsolutaMethod(List<string> candidatosValidos, decimal umbral = 0.5m)
    {
        _candidatosValidos = candidatosValidos;
        _umbral = umbral;
    }

    public bool ValidarVoto(Voto voto)
    {
        voto.Tipo = Voto.Clasificar(voto, _candidatosValidos);
        return true; // todos se registran; nulos/no-marcados se excluyen del denominador
    }

    public void ContabilizarVoto(Voto voto)
    {
        _acta.RegistrarVoto(voto.Tipo);
        _urna.Add(voto);
    }

    public Resultado CalcularResultado()
    {
        // US-04: denominador = válidos + blancos (excluye nulos y no marcados)
        int denominador = _acta.VotosValidos + _acta.VotosEnBlanco;

        var conteo = _candidatosValidos.ToDictionary(c => c, _ => 0m);

        foreach (var voto in _urna.Where(v => v.Tipo == TipoVoto.Valido))
        {
            var candidato = voto.Preferencias.OrderBy(p => p.Value).First().Key;
            if (conteo.ContainsKey(candidato))
                conteo[candidato]++;
        }

        var ordenado  = conteo.OrderByDescending(x => x.Value).ToList();
        var primero   = ordenado[0];
        var resultado = new Resultado { Totales = conteo };

        // US-04: voto en blanco supera al candidato más votado
        if (_acta.VotosEnBlanco > primero.Value)
        {
            RepeticionEleccion = true;
            _acta.Eventos.Add("[FLAG] repeticion_eleccion activado: voto en blanco supera al ganador.");
            resultado.Ganador = "__REPETICION__";
            return resultado;
        }

        // US-04: umbral 50%+1
        decimal umbralVotos = Math.Floor(denominador * _umbral) + 1;

        if (primero.Value >= umbralVotos)
        {
            resultado.Ganador    = primero.Key;
            resultado.Porcentaje = denominador > 0 ? primero.Value / denominador : 0;
            return resultado;
        }

        // US-04: nadie supera el umbral → segunda vuelta
        RequiereSegundaVuelta = true;
        FinalistasSegundaVuelta = ordenado.Take(2).Select(x => x.Key).ToList();
        _acta.Eventos.Add($"[FLAG] requiere_segunda_vuelta: {string.Join(" vs ", FinalistasSegundaVuelta)}");

        resultado.Ganador = "__SEGUNDA_VUELTA__";
        return resultado;
    }

    public Acta GenerarActa() => _acta;
}