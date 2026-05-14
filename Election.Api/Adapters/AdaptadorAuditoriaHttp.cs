using System.Net.Http.Json;
using System.Text.Json;
using Election.Core.Interfaces;
using Election.Core.Models;

namespace Election.Api.Adapters;

// Adaptador real del puerto SR-M6.
// Envía cada evento al transparency-service vía POST.
//
// Usa IHttpClientFactory con cliente nombrado "transparency" para evitar
// el problema de "cannot consume scoped service from singleton" — este
// adaptador se registra como singleton y resuelve el HttpClient bajo demanda.
//
// URL base configurable en appsettings.json:
//   "TransparencyService": { "Url": "http://transparency-service:8084" }
public class AdaptadorAuditoriaHttp : IPuertoAuditoriaSrM6
{
    public const string HTTP_CLIENT_NAME = "transparency";

    private readonly IHttpClientFactory _factory;
    private readonly ILogger<AdaptadorAuditoriaHttp> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
    };

    private const string PATH = "/api/v1/transparency/events";

    public AdaptadorAuditoriaHttp(
        IHttpClientFactory factory,
        ILogger<AdaptadorAuditoriaHttp> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public void RegistrarEvento(EventoAuditoriaSrM6 evento)
    {
        var http = _factory.CreateClient(HTTP_CLIENT_NAME);

        try
        {
            var response = http.PostAsJsonAsync(PATH, evento, JsonOptions)
                .GetAwaiter()
                .GetResult();

            if (!response.IsSuccessStatusCode)
            {
                string body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                _logger.LogError(
                    "[SR-M6] transparency-service rechazó el evento {EventType} con {Status}: {Body}",
                    evento.EventType, (int)response.StatusCode, body);
                throw new InvalidOperationException(
                    $"transparency-service respondió {(int)response.StatusCode}. " +
                    $"Verifique política Zero-Identity en details. Detalle: {body}");
            }

            _logger.LogInformation(
                "[SR-M6] evento {EventType} registrado en transparency-service.",
                evento.EventType);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "[SR-M6] no se pudo contactar transparency-service en {BaseAddress}",
                http.BaseAddress);
            throw new InvalidOperationException(
                "No fue posible registrar el evento en transparency-service.", ex);
        }
    }
}
