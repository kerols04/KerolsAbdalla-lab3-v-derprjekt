using System;
using System.Collections.Generic;
using System.Linq;
using VaderProjekt.Core.Entities;

namespace VaderProjekt.Core.Services
{
    /// <summary>
    /// Beräknar meteorologisk höst och vinter från UTOMHUS-data.
    /// Vi använder dygnsmedeltemperatur och letar efter 5 dygn i rad som uppfyller villkoret.
    /// Viktigt: om det finns luckor i kalendern räknas det inte som "i rad".
    /// </summary>
    public static class MeteorologiskaArstider
    {
        // Liten intern typ för att bära (Datum, MedelTemp) per dag.
        private sealed record DagMedel(DateTime Datum, double MedelTemp);

        /// <summary>
        /// Meteorologisk höst: dygnsmedeltemp < 10°C fem dygn i följd.
        /// (Tidigast 1 augusti enligt vanlig definition.)
        /// Returnerar första dagen i den femdagarsperioden, annars null.
        /// </summary>
        public static DateTime? HittaHostDatum(IEnumerable<VaderData> uteData)
        {
            // 1) Dygnsmedel för varje dag (ute)
            var perDag = uteData
                .GroupBy(v => v.Datum.Date)
                .Select(g => new DagMedel(g.Key, g.Average(x => x.Temp)))
                .OrderBy(x => x.Datum)
                .ToList();

            if (!perDag.Any()) return null;

            // 2) Höst kan inte starta före 1 augusti
            var startDatum = new DateTime(perDag.First().Datum.Year, 8, 1);
            var kandidater = perDag.Where(x => x.Datum >= startDatum).ToList();

            // 3) Hitta första sekvens av 5 dygn i rad med medeltemp < 10
            return HittaForstaSekvens(kandidater, krav: d => d.MedelTemp < 10.0);
        }

        /// <summary>
        /// Meteorologisk vinter: dygnsmedeltemp ≤ 0°C fem dygn i följd.
        /// Returnerar första dagen i den femdagarsperioden, annars null.
        /// </summary>
        public static DateTime? HittaVinterDatum(IEnumerable<VaderData> uteData)
        {
            var perDag = uteData
                .GroupBy(v => v.Datum.Date)
                .Select(g => new DagMedel(g.Key, g.Average(x => x.Temp)))
                .OrderBy(x => x.Datum)
                .ToList();

            if (!perDag.Any()) return null;

            return HittaForstaSekvens(perDag, krav: d => d.MedelTemp <= 0.0);
        }

        /// <summary>
        /// Gemensam logik: leta efter första femdagarsperioden som:
        /// 1) är 5 kalenderdygn i rad (inga luckor)
        /// 2) alla dagar uppfyller "krav"
        /// </summary>
        private static DateTime? HittaForstaSekvens(IReadOnlyList<DagMedel> dagar, Func<DagMedel, bool> krav)
        {
            // Behöver minst 5 dagar för att ens kunna hitta en period
            if (dagar.Count < 5) return null;

            for (int i = 0; i <= dagar.Count - 5; i++)
            {
                var period = dagar.Skip(i).Take(5).ToList();

                // Kolla att dagarna verkligen är på varandra följande datum
                bool femIDrad = true;
                for (int j = 1; j < period.Count; j++)
                {
                    if (period[j].Datum != period[0].Datum.AddDays(j))
                    {
                        femIDrad = false;
                        break;
                    }
                }

                if (!femIDrad)
                    continue;

                // Om alla 5 dagar uppfyller villkoret så hittade vi startdatumet
                if (period.All(krav))
                    return period[0].Datum;
            }

            return null;
        }
    }
}
