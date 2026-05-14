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
builder.Services.AddSingleton<IPuertoAuditoriaSrM6, AdaptadorAuditoriaLog>();
builder.Services.AddSingleton<IPuertoEmailCertificado, AdaptadorEmailLog>();
builder.Services.AddSingleton<IServicioEmisionVoto, ServicioEmisionVoto>();

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
