using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VaderProjekt.Core.Entities;

namespace VaderProjekt.DataAccess.Seeding
{
    /// <summary>
    /// Skapar databasen om den inte finns och läser in CSV-data vid första körningen.
    /// Filen är autentisk och kan innehålla datafel (t.ex. unicode-minus i temperatur).
    /// </summary>
    public static class DatabaseInitializer
    {
        private static readonly string[] DatumFormat =
        {
            "yyyy-MM-dd H:mm",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd H:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
        };

        public static void EnsureCreatedAndSeeded(VaderContext db, string csvPath)
        {
            // Skapa DB (Code First)
            db.Database.EnsureCreated();

            // Om vi redan har data, gör ingenting.
            if (db.VaderDataTabell.AsNoTracking().Any())
                return;

            var data = ReadCsv(csvPath);
            if (data.Count == 0)
                return;

            // Snabbare bulk-insert
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.VaderDataTabell.AddRange(data);
            db.SaveChanges();
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        /// <summary>
        /// Läser TempFuktData.csv. Validerar + korrigerar uppenbara fel.
        /// - Temperatur kan ibland ha "−" (unicode minus) istället för "-".
        /// - Luftfuktighet klampas till 0..100.
        /// - Orimliga temperaturer filtreras bort.
        /// </summary>
        private static List<VaderData> ReadCsv(string csvPath)
        {
            var list = new List<VaderData>(capacity: 160_000);

            if (!File.Exists(csvPath))
                return list;

            // För att undvika dubbletter.
            var seen = new HashSet<(DateTime Datum, string Plats)>();

            foreach (var (line, index) in File.ReadLines(csvPath).Select((l, i) => (l, i)))
            {
                if (index == 0) continue; // hoppa över header
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 4) continue;

                if (!TryParseDatum(parts[0], out var datum)) continue;

                var plats = (parts[1] ?? string.Empty).Trim();
                if (plats is not ("Ute" or "Inne")) continue;

                var tempText = (parts[2] ?? string.Empty).Trim()
                    .Replace('−', '-')  // unicode minus -> ASCII
                    .Replace(" ", "");

                if (!double.TryParse(tempText, NumberStyles.Float, CultureInfo.InvariantCulture, out var temp))
                    continue;

                // Rimlighetsintervall (uppgiften säger att data kan innehålla fel)
                if (temp < -50 || temp > 60)
                    continue;

                if (!int.TryParse((parts[3] ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rh))
                    continue;

                rh = Math.Clamp(rh, 0, 100);

                var key = (datum, plats);
                if (!seen.Add(key))
                    continue;

                list.Add(new VaderData
                {
                    Datum = datum,
                    Plats = plats,
                    Temp = temp,
                    Luftfuktighet = rh
                });
            }

            return list;
        }

        private static bool TryParseDatum(string text, out DateTime datum)
        {
            text = (text ?? string.Empty).Trim();

            return DateTime.TryParseExact(
                text,
                DatumFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out datum
            );
        }
    }
}
