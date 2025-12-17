using Microsoft.EntityFrameworkCore;
using VaderProjekt.Core.Entities;

namespace VaderProjekt.DataAccess
{
    /// <summary>
    /// DbContext för Väderprojektet.
    /// Code First: databasen skapas automatiskt vid första körningen.
    /// Vi använder SQLite så projektet fungerar enkelt på alla datorer.
    /// </summary>
    public sealed class VaderContext : DbContext
    {
        // Tabell med alla mätningar (en rad = en mätpunkt från CSV)
        public DbSet<VaderData> VaderDataTabell => Set<VaderData>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Databasfil i programmets arbetskatalog.
            // Namnet innehåller "vaderprojekt" enligt uppgiftskravet.
            optionsBuilder.UseSqlite("Data Source=vaderprojekt.sqlite");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Sätt ett tydligt tabellnamn
            modelBuilder.Entity<VaderData>().ToTable("VaderData");

            // Plats måste vara ifylld ("Ute" eller "Inne")
            modelBuilder.Entity<VaderData>()
                .Property(v => v.Plats)
                .IsRequired();

            // Index för snabbare sökning/sortering (utan att riskera insert-fel på dubbletter).
            modelBuilder.Entity<VaderData>()
                .HasIndex(v => new { v.Datum, v.Plats });
        }
    }
}
