using Election.Api.Adapters;
using Election.Core.Interfaces;
using Election.Engine.Methods.AlternativeVote;
using Election.Engine.Methods.MayoriaSimple;
using Election.VoteVault.Services;
using Election.VoteVault.Workers;
using Election.VoteVault.Ceremony.Interfaces;
using Election.VoteVault.Ceremony.Services;
using Election.VoteVault.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMetodoElectoral, AlternativeVoteMethod>();

builder.Services.AddSingleton<IVoteVaultService,VoteVaultService>();

builder.Services.AddSingleton<ISealService, SealService>();

builder.Services.AddHostedService<VaultHeartbeatWorker>();

builder.Services.AddSingleton<IMetodoElectoral>(
    new MayoriaSimpleMethod(new List<string> { "juan_pablo", "elena", "ricardo" })
);


builder.Services.AddSingleton<
    IOpeningCeremonyService,
    OpeningCeremonyService
>();

// SE-M3-01 / SE-M3-02: emisión de voto (presencial y remoto).
builder.Services.AddSingleton<IServicioFirmaDigital, ServicioFirmaDigital>();
builder.Services.AddSingleton<IGeneradorVvpat, GeneradorVvpatTexto>();
builder.Services.AddSingleton<IPuertoEmailCertificado, AdaptadorEmailLog>();
builder.Services.AddSingleton<IServicioEmisionVoto, ServicioEmisionVoto>();

// SR-M6 (transparency-service): bus HTTP centralizado para auditoría.
// URL configurable en appsettings (TransparencyService:Url).
string transparencyUrl =
    builder.Configuration["TransparencyService:Url"]
    ?? throw new InvalidOperationException(
        "Falta TransparencyService:Url en la configuración.");

builder.Services
    .AddHttpClient(AdaptadorAuditoriaHttp.HTTP_CLIENT_NAME, client =>
    {
        client.BaseAddress = new Uri(transparencyUrl);
        client.Timeout = TimeSpan.FromSeconds(5);
    });

builder.Services.AddSingleton<IPuertoAuditoriaSrM6, AdaptadorAuditoriaHttp>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Controllers
app.MapControllers();

app.Run();
