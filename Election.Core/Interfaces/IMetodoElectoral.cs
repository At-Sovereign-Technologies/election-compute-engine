namespace Election.Core.Interfaces;

using Election.Core.Models;

public interface IMetodoElectoral
{
    bool ValidarVoto(Voto voto);

    void ContabilizarVoto(Voto voto);

    Resultado CalcularResultado();

    bool RequiereSegundaVuelta();

    Acta GenerarActa();
}