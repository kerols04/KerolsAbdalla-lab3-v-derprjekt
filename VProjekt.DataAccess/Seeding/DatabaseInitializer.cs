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
    /// CSV-filen är autentisk och kan innehålla fel/luckor → vi hoppar över trasiga rader istället för att krascha.
    /// </summary>
    public static class DatabaseInitializer
    {
        // Vanliga datumformat som kan förekomma i filen
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

            // Om det redan finns data: seed inte igen
            if (db.VaderDataTabell.AsNoTracking().Any())
                return;

            var data = ReadCsv(csvPath);
            if (data.Count == 0)
                return;

            // Lite snabbare insert när det är många rader
            db.ChangeTracker.AutoDetectChangesEnabled = false;
            db.VaderDataTabell.AddRange(data);
            db.SaveChanges();
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }

        /// <summary>
        /// Läser TempFuktData.csv:
        /// - Hoppar över header
        /// - Klarar unicode-minus (−) i temperatur
        /// - Klamp: RH 0..100
        /// - Rimlighetsfilter: temp -50..60
        /// - Undviker dubbletter (Datum+Plats)
        /// </summary>
        private static List<VaderData> ReadCsv(string csvPath)
        {
            var list = new List<VaderData>(capacity: 160_000);

            if (!File.Exists(csvPath))
                return list;

            // För att slippa lägga in samma mätning flera gånger
            var seen = new HashSet<(DateTime Datum, string Plats)>();

            foreach (var (line, index) in File.ReadLines(csvPath).Select((l, i) => (l, i)))
            {
                if (index == 0) continue; // header
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 4) continue;

                // 1) Datum
                if (!TryParseDatum(parts[0], out var datum))
                    continue;

                // 2) Plats (bara Ute/Inne accepteras)
                var plats = (parts[1] ?? string.Empty).Trim();
                if (plats is not ("Ute" or "Inne"))
                    continue;

                // 3) Temperatur (kan ha unicode-minus)
                var tempText = (parts[2] ?? string.Empty).Trim()
                    .Replace('−', '-')   // unicode minus → vanlig minus
                    .Replace(" ", "");

                if (!double.TryParse(tempText, NumberStyles.Float, CultureInfo.InvariantCulture, out var temp))
                    continue;

                // Rimlighetsfilter (data kan innehålla fel)
                if (temp < -50 || temp > 60)
                    continue;

                // 4) Luftfuktighet
                if (!int.TryParse((parts[3] ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rh))
                    continue;

                rh = Math.Clamp(rh, 0, 100);

                // Dubblettskydd (samma tid + plats ska normalt bara finnas en gång)
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

            // Vi läser med invariant culture för att vara stabila oavsett datorns språk
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
