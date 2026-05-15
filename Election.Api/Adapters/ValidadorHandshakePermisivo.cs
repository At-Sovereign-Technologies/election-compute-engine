using Election.Core.Interfaces;

namespace Election.Api.Adapters;

// Implementación temporal mientras SE-M2 no expone su servicio de handshake.
// Acepta cualquier handshakeId no vacío y registra advertencia.
// Reemplazar por ValidadorHandshakeHttp cuando exista el servicio real.
public class ValidadorHandshakePermisivo : IValidadorHandshake
{
    private readonly ILogger<ValidadorHandshakePermisivo> _logger;

    public ValidadorHandshakePermisivo(ILogger<ValidadorHandshakePermisivo> logger)
    {
        _logger = logger;
    }

    public bool EsValido(string handshakeId)
    {
        bool valido = !string.IsNullOrWhiteSpace(handshakeId);
        if (valido)
        {
            _logger.LogWarning(
                "[HANDSHAKE] aceptado por validador permisivo (pendiente integración SE-M2). id={Id}",
                handshakeId);
        }
        return valido;
    }
}
