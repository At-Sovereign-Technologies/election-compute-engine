using Election.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Election.Api.Data;

// Contexto EF para la persistencia de registros de asistencia (US-SE-M3-05).
// SQLite por defecto (archivo local sello_jornada.db); intercambiable a Postgres
// cambiando el provider en Program.cs.
public class AsistenciaDbContext : DbContext
{
    public AsistenciaDbContext(DbContextOptions<AsistenciaDbContext> options)
        : base(options) {}

    public DbSet<RegistroAsistencia> Registros => Set<RegistroAsistencia>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var e = b.Entity<RegistroAsistencia>();
        e.ToTable("acompanantes_jornada");
        e.HasKey(x => x.Id);
        e.Property(x => x.HashDocVotante).HasMaxLength(64).IsRequired();
        e.Property(x => x.HashDocAcompanante).HasMaxLength(64).IsRequired();
        e.Property(x => x.MesaId).HasMaxLength(64).IsRequired();
        e.Property(x => x.JornadaId).HasMaxLength(64).IsRequired();
        e.Property(x => x.JuradoId).HasMaxLength(64).IsRequired();
        e.Property(x => x.SesionToken).HasMaxLength(512).IsRequired();
        e.Property(x => x.Estado).HasMaxLength(20).IsRequired();
        e.Property(x => x.TipoAsistencia).HasConversion<string>().HasMaxLength(20);

        // Índice clave para el control de duplicados por jornada.
        e.HasIndex(x => new { x.HashDocAcompanante, x.JornadaId });
        // Índice para acumulado por mesa.
        e.HasIndex(x => x.MesaId);
        // Búsqueda rápida del token al emitir voto.
        e.HasIndex(x => x.SesionToken).IsUnique();
    }
}
