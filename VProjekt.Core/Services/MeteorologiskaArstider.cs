using System;
using System.Collections.Generic;
using System.Linq;
using VaderProjekt.Core.Entities;

namespace VaderProjekt.Core.Services
{
    /// <summary>
    /// Beräknar meteorologisk höst/vinter utifrån SMHI-liknande regler.
    /// Kravet i uppgiften är att ni kan dra slutsatser av dygnsmedeltemperatur.
    /// </summary>
    public static class MeteorologiskaArstider
    {
        private sealed record DagMedel(DateTime Datum, double MedelTemp);

        /// <summary>
        /// Meteorologisk höst: dygnsmedeltemp < 10°C fem dygn i följd.
        /// (Tidigast 1 augusti.)
        /// </summary>
        public static DateTime? HittaHostDatum(IEnumerable<VaderData> uteData)
        {
            var perDag = uteData
                .GroupBy(v => v.Datum.Date)
                .Select(g => new DagMedel(g.Key, g.Average(x => x.Temp)))
                .OrderBy(x => x.Datum)
                .ToList();

            if (!perDag.Any()) return null;

            var startDatum = new DateTime(perDag.First().Datum.Year, 8, 1);
            var kandidater = perDag.Where(x => x.Datum >= startDatum).ToList();

            return HittaForstaSekvens(kandidater, krav: d => d.MedelTemp < 10.0);
        }

        /// <summary>
        /// Meteorologisk vinter: dygnsmedeltemp ≤ 0°C fem dygn i följd.
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

        private static DateTime? HittaForstaSekvens(IReadOnlyList<DagMedel> dagar, Func<DagMedel, bool> krav)
        {
            // Vi kräver även att dagarna är på varandra följande (inga luckor i kalendern).
            for (int i = 0; i <= dagar.Count - 5; i++)
            {
                var period = dagar.Skip(i).Take(5).ToList();

                bool femIDrad = true;
                for (int j = 1; j < period.Count; j++)
                {
                    if (period[j].Datum != period[0].Datum.AddDays(j))
                    {
                        femIDrad = false;
                        break;
                    }
                }

                if (!femIDrad) continue;

                if (period.All(krav))
                    return period[0].Datum;
            }

            return null;
        }
    }
}
