using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using VaderProjekt.Core.Entities;
using VaderProjekt.Core.Models;
using VaderProjekt.Core.Services;
using VaderProjekt.DataAccess;
using VaderProjekt.DataAccess.Seeding;

namespace VaderProjekt.UI
{
    internal static class Program
    {
        private static void Main()
        {
            // För att kunna skriva ut svenska tecken i konsolen (ÅÄÖ)
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("====================================");
            Console.WriteLine("           VÄDERPROJEKT             ");
            Console.WriteLine("====================================\n");

            // Hitta CSV-fil på typiska platser (output, current directory, projektmapp)
            var csvPath = HittaCsvFil();
            if (csvPath is null)
            {
                Console.WriteLine("Hittar inte TempFuktData.csv.");
                Console.WriteLine("Lägg filen i samma mapp som exe-filen eller i projektmappen för UI.");
                Avsluta();
                return;
            }

            // Skapa DB + seed:a bara om DB är tom
            using var db = new VaderContext();
            DatabaseInitializer.EnsureCreatedAndSeeded(db, csvPath);

            // Läs in all data som en lista (UI arbetar på minnesdata)
            var alla = db.VaderDataTabell.AsNoTracking().ToList();
            Console.WriteLine($"Databas klar. Antal mätningar: {alla.Count:N0}\n");

            // Enkel menyloop
            while (true)
            {
                SkrivMeny();
                Console.Write("Välj: ");
                var val = (Console.ReadLine() ?? string.Empty).Trim();

                switch (val)
                {
                    case "1":
                        Meny_MedeltempForDatum(alla);
                        break;

                    case "2":
                        Meny_SorteraMedeltemp(alla);
                        break;

                    case "3":
                        Meny_SorteraMedelFukt(alla);
                        break;

                    case "4":
                        Meny_SorteraMogelRisk(alla);
                        break;

                    case "5":
                        Meny_MeteorologiskaArstider(alla);
                        break;

                    case "6":
                        Meny_InneUteSkillnad(alla);
                        break;

                    case "7":
                        Meny_BalkongdorrOppet(alla);
                        break;

                    case "0":
                        Avsluta();
                        return;

                    default:
                        Console.WriteLine("Ogiltigt val. Försök igen.\n");
                        break;
                }
            }
        }

        private static void SkrivMeny()
        {
            Console.WriteLine("\n--- MENY ---");
            Console.WriteLine("1) Medeltemperatur för valt datum (ute/inne)");
            Console.WriteLine("2) Sortera dagar efter medeltemperatur (varmast↔kallast)");
            Console.WriteLine("3) Sortera dagar efter medelluftfuktighet (torrast↔fuktigast)");
            Console.WriteLine("4) Sortera dagar efter mögelrisk (minst↔mest)");
            Console.WriteLine("5) Meteorologisk höst/vinter (ute)");
            Console.WriteLine("6) [VG] Dagar då inne/ute skiljer sig mest/minst");
            Console.WriteLine("7) [VG] Balkongdörr öppen - sortera dagar");
            Console.WriteLine("0) Avsluta\n");
        }

        private static void Meny_MedeltempForDatum(List<VaderData> alla)
        {
            var datum = FragaDatum("Ange datum (YYYY-MM-DD): ");
            if (datum is null) return;

            var plats = FragaPlats();
            if (plats is null)
            {
                Console.WriteLine("Ogiltig plats. Välj 1 eller 2.");
                return;
            }

            var medel = VaderAnalys.MedelTemperaturForDatum(alla, datum.Value, plats);
            if (medel is null)
            {
                Console.WriteLine("Inga mätningar hittades för det datumet/platsen.");
                return;
            }

            Console.WriteLine($"Medeltemperatur {datum:yyyy-MM-dd} ({plats}): {medel.Value:F1} °C");
        }

        private static void Meny_SorteraMedeltemp(List<VaderData> alla)
        {
            var plats = FragaPlats();
            if (plats is null)
            {
                Console.WriteLine("Ogiltig plats. Välj 1 eller 2.");
                return;
            }

            Console.Write("Sortera varmast först? (j/n): ");
            var varmastForst = SvarJaNej(defaultYes: true);

            var lista = VaderAnalys.SorteraDagarEfterMedelTemp(alla, plats, varmastForst);
            SkrivTopplista(lista, rubrik: $"Medeltemp per dag ({plats})", antal: 10, enhet: "°C");
        }

        private static void Meny_SorteraMedelFukt(List<VaderData> alla)
        {
            var plats = FragaPlats();
            if (plats is null)
            {
                Console.WriteLine("Ogiltig plats. Välj 1 eller 2.");
                return;
            }

            Console.Write("Sortera torrast först? (j/n): ");
            var torrastForst = SvarJaNej(defaultYes: true);

            var lista = VaderAnalys.SorteraDagarEfterMedelFukt(alla, plats, torrastForst);
            SkrivTopplista(lista, rubrik: $"Medelluftfuktighet per dag ({plats})", antal: 10, enhet: "%");
        }

        private static void Meny_SorteraMogelRisk(List<VaderData> alla)
        {
            var plats = FragaPlats();
            if (plats is null)
            {
                Console.WriteLine("Ogiltig plats. Välj 1 eller 2.");
                return;
            }

            Console.Write("Sortera minst risk först? (j/n): ");
            var minstForst = SvarJaNej(defaultYes: true);

            var lista = VaderAnalys.SorteraDagarEfterMogelRisk(alla, plats, minstForst);
            SkrivTopplista(lista, rubrik: $"Mögelrisk per dag ({plats})", antal: 10, enhet: "% av dagen i riskzon");
        }

        private static void Meny_MeteorologiskaArstider(List<VaderData> alla)
        {
            // Årstider räknas på utomhusdata (dygnsmedel)
            var ute = alla.Where(x => x.Plats == "Ute");

            var host = MeteorologiskaArstider.HittaHostDatum(ute);
            var vinter = MeteorologiskaArstider.HittaVinterDatum(ute);

            Console.WriteLine("\nMeteorologiska årstider (ute, dygnsmedel):");

            Console.WriteLine(host is null
                ? "- HÖST: hittades inte i mätperioden."
                : $"- HÖST: {host:yyyy-MM-dd} (första av 5 dygn < 10°C)");

            Console.WriteLine(vinter is null
                ? "- VINTER: hittades inte i mätperioden (mild vinter / för kort period)."
                : $"- VINTER: {vinter:yyyy-MM-dd} (första av 5 dygn ≤ 0°C)");
        }

        private static void Meny_InneUteSkillnad(List<VaderData> alla)
        {
            Console.Write("Visa mest skillnad först? (j/n): ");
            var mestForst = SvarJaNej(defaultYes: true);

            var lista = VaderAnalys.SorteraDagarEfterInneUteSkillnad(alla, mestForst);
            SkrivTopplista(lista, rubrik: "[VG] Inne/Ute-skillnad per dag (medel |Inne-Ute|)", antal: 10, enhet: "°C");
        }

        private static void Meny_BalkongdorrOppet(List<VaderData> alla)
        {
            Console.WriteLine("\n[VG] Balkongdörr öppen - uppskattning per dag");
            Console.WriteLine("Antagande: Vid öppning sjunker innertemperaturen snabbt och utetemperaturen stiger snabbt.\n");

            var lista = VaderAnalys.SorteraDagarEfterBalkongDorrOppetTid(alla);

            foreach (var r in lista.Take(10))
                Console.WriteLine(r);

            if (lista.Count > 10)
                Console.WriteLine($"... ({lista.Count} dagar totalt)");
        }

        private static void SkrivTopplista(
            IReadOnlyList<DagligtResultat> lista,
            string rubrik,
            int antal,
            string enhet)
        {
            Console.WriteLine($"\n{rubrik}");
            Console.WriteLine(new string('-', rubrik.Length));

            foreach (var rad in lista.Take(antal))
                Console.WriteLine($"{rad.Datum:yyyy-MM-dd}: {rad.Varde:F1} {enhet}");

            if (lista.Count > antal)
                Console.WriteLine($"... ({lista.Count} dagar totalt)\n");
        }

        private static DateTime? FragaDatum(string prompt)
        {
            Console.Write(prompt);
            var text = (Console.ReadLine() ?? string.Empty).Trim();

            // Vi kräver YYYY-MM-DD för att undvika missförstånd
            if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                return d.Date;

            Console.WriteLine("Fel format. Använd YYYY-MM-DD.");
            return null;
        }

        private static string? FragaPlats()
        {
            Console.Write("Plats (1 = Ute, 2 = Inne): ");
            var text = (Console.ReadLine() ?? string.Empty).Trim();

            return text switch
            {
                "1" => "Ute",
                "2" => "Inne",
                _ => null
            };
        }

        private static bool SvarJaNej(bool defaultYes)
        {
            var s = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(s)) return defaultYes;

            return s is "j" or "ja" or "y" or "yes";
        }

        private static string? HittaCsvFil()
        {
            // 1) Vanligast: filen kopieras till output via csproj (CopyToOutputDirectory)
            var p1 = System.IO.Path.Combine(AppContext.BaseDirectory, "TempFuktData.csv");
            if (System.IO.File.Exists(p1)) return p1;

            // 2) Om man kör från projektmappen
            var p2 = System.IO.Path.Combine(Environment.CurrentDirectory, "TempFuktData.csv");
            if (System.IO.File.Exists(p2)) return p2;

            // 3) Relativ sökväg från bin/Debug/... tillbaka till projektmappen
            var p3 = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TempFuktData.csv"));
            if (System.IO.File.Exists(p3)) return p3;

            return null;
        }

        private static void Avsluta()
        {
            Console.WriteLine("\nTryck valfri tangent för att avsluta...");
            Console.ReadKey();
        }
    }
}
