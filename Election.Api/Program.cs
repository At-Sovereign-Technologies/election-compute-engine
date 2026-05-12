using Election.Api.Services;
using Election.Core.Interfaces;
using Election.Engine.Methods.AlternativeVote;
using Election.Engine.Scrutiny;
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

// Configure HttpClient for Transparency Service
var transparencyServiceUrl = builder.Configuration["TransparencyService:BaseUrl"] 
    ?? "http://localhost:8080";
var transparencyTimeout = int.Parse(
    builder.Configuration["TransparencyService:Timeout"] ?? "5000"
);

builder.Services
    .AddHttpClient<ITransparencyAuditService, TransparencyAuditService>(client =>
    {
        client.BaseAddress = new Uri(transparencyServiceUrl);
        client.Timeout = TimeSpan.FromMilliseconds(transparencyTimeout);
    })
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("Content-Type", "application/json");
    });

// Register audit and scrutiny services
builder.Services.AddSingleton<IHandshakeService, HandshakeService>();
builder.Services.AddSingleton<IScrutinyAuditor, ScrutinyAuditor>();

builder.Services.AddSingleton<IMetodoElectoral, AlternativeVoteMethod>();

builder.Services.AddSingleton<IVoteVaultService,VoteVaultService>();

builder.Services.AddSingleton<ISealService, SealService>();

builder.Services.AddHostedService<VaultHeartbeatWorker>();

builder.Services.AddSingleton<
    IOpeningCeremonyService,
    OpeningCeremonyService
>();

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