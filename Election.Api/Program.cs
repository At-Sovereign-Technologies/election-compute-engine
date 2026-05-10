using Election.Core.Interfaces;
using Election.Engine.Methods.AlternativeVote;
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