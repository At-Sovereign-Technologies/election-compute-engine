using Election.Api.Adapters;
using Election.Api.Data;
using Election.Api.Services;
using Election.Core.Interfaces;
using Election.Engine.Methods.AlternativeVote;
using Election.Engine.Methods.MayoriaSimple;
using Election.Engine.Scrutiny;
using Election.VoteVault.Services;
using Election.VoteVault.Workers;
using Election.VoteVault.Ceremony.Interfaces;
using Election.VoteVault.Ceremony.Services;
using Election.VoteVault.Interfaces;
using Microsoft.EntityFrameworkCore;
using SendGrid;
using SendGrid.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Servicios
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Allow cross-origin requests from any origin (permissive for local testing).
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// SR-M6 (auditoría distribuida): HttpClient del servicio de transparencia.
// El compañero (PR auditoria-SR-M6) introduce ITransparencyAuditService;
// nosotros (SE-M3-01..05) consumimos el bus vía IPuertoAuditoriaSrM6
// con un HttpClient nombrado. Ambas integraciones conviven hasta que el
// equipo consolide una única abstracción de auditoría.
var transparencyServiceUrl = builder.Configuration["TransparencyService:BaseUrl"]
    ?? builder.Configuration["TransparencyService:Url"]
    ?? "http://localhost:8084";
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
    });

// Handshake + escrutinio (mantenidos del PR de auditoría SR-M6).
builder.Services.AddSingleton<IHandshakeService, HandshakeService>();
builder.Services.AddSingleton<IScrutinyAuditor, ScrutinyAuditor>();

builder.Services.AddSingleton<IMetodoElectoral, AlternativeVoteMethod>();

builder.Services.AddSingleton<VoteVaultService>();
builder.Services.AddSingleton<IVoteVaultService>(sp => sp.GetRequiredService<VoteVaultService>());

builder.Services.AddSingleton<SealService>();
builder.Services.AddSingleton<ISealService>(sp => sp.GetRequiredService<SealService>());

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

// Punto de extensión: validación del handshake del votante presencial.
// Reemplazar ValidadorHandshakePermisivo por ValidadorHandshakeHttp cuando
// SE-M2 exponga el servicio real.
builder.Services.AddSingleton<IValidadorHandshake, ValidadorHandshakePermisivo>();

// SE-M3-02: correo certificado. Selección de proveedor por configuración:
//   Email:Provider = "SendGrid" | "Smtp" | "Log"
// Sin configuración explícita usa "Log" (no envía, solo registra; útil para CI).
string emailProvider = builder.Configuration["Email:Provider"] ?? "Log";
switch (emailProvider.ToLowerInvariant())
{
    case "sendgrid":
        string sendGridKey = builder.Configuration["Email:SendGrid:ApiKey"]
            ?? throw new InvalidOperationException(
                "Email:Provider=SendGrid pero falta Email:SendGrid:ApiKey.");
        builder.Services.AddSendGrid(opts => opts.ApiKey = sendGridKey);
        builder.Services.AddSingleton<IPuertoEmailCertificado, AdaptadorEmailSendGrid>();
        break;
    case "smtp":
        builder.Services.AddSingleton<IPuertoEmailCertificado, AdaptadorEmailSmtp>();
        break;
    default:
        builder.Services.AddSingleton<IPuertoEmailCertificado, AdaptadorEmailLog>();
        break;
}

builder.Services.AddSingleton<IServicioEmisionVoto, ServicioEmisionVoto>();

// SR-M6 (transparency-service): bus HTTP usado desde SE-M3 vía adaptador propio.
// URL configurable en appsettings (TransparencyService:Url o TransparencyService:BaseUrl).
builder.Services
    .AddHttpClient(AdaptadorAuditoriaHttp.HTTP_CLIENT_NAME, client =>
    {
        client.BaseAddress = new Uri(transparencyServiceUrl);
        client.Timeout = TimeSpan.FromMilliseconds(transparencyTimeout);
    });

builder.Services.AddSingleton<IPuertoAuditoriaSrM6, AdaptadorAuditoriaHttp>();

// SE-M3-05: persistencia local de registros de asistencia (SQLite).
// Provider intercambiable a Postgres con un solo cambio aquí.
string asistenciaConn =
    builder.Configuration.GetConnectionString("Asistencia")
    ?? "Data Source=sello_jornada.db";

builder.Services.AddDbContextFactory<AsistenciaDbContext>(opts =>
    opts.UseSqlite(asistenciaConn));

builder.Services.AddSingleton<IServicioAsistencia, ServicioAsistencia>();

var app = builder.Build();

// SE-M3-05: crear la BD en el arranque si no existe. EnsureCreated es suficiente
// para SQLite en entorno académico. Si se cambia a Postgres en producción, aquí
// se reemplaza por db.Database.MigrateAsync() con migraciones EF formales.
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<AsistenciaDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();
}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();

// Controllers
app.MapControllers();

app.Run();
