using Microsoft.EntityFrameworkCore;
using VaderProjekt.Core.Entities;

namespace VaderProjekt.DataAccess
{
    /// <summary>
    /// DbContext för väderprojektet. Code First (EF) skapar databasen automatiskt.
    /// Vi använder SQLite för att det ska vara lätt att köra på vilken dator som helst.
    /// </summary>
    public sealed class VaderContext : DbContext
    {
        public DbSet<VaderData> VaderDataTabell { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Databasfilen hamnar i programmets arbetskatalog (t.ex. bin/Debug/...)
            // Namnet innehåller "vaderprojekt" enligt uppgiftskravet.
            optionsBuilder.UseSqlite("Data Source=vaderprojekt.sqlite");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VaderData>().ToTable("VaderData");
            modelBuilder.Entity<VaderData>().Property(v => v.Plats).IsRequired();

            // Hjälper mot dubbletter i data (samma tidpunkt+plats bör vara unik).
            modelBuilder.Entity<VaderData>()
                .HasIndex(v => new { v.Datum, v.Plats })
                .IsUnique(false);
        }
    }
}
