using System;
using System.Collections.Generic;
using System.Linq;
using VaderProjekt.Core.Entities;
using VaderProjekt.Core.Models;

namespace VaderProjekt.Core.Services
{
    /// <summary>
    /// All analys/beräkningslogik ligger här (Core).
    /// UI ska bara anropa metoder och skriva ut resultat.
    /// </summary>
    public static class VaderAnalys
    {
        public static double? MedelTemperaturForDatum(IEnumerable<VaderData> data, DateTime datum, string plats)
        {
            // Filtrera på plats + valt datum (dygn)
            var query = data
                .Where(v => v.Plats == plats && v.Datum.Date == datum.Date)
                .Select(v => (double?)v.Temp);

            // Om ingen data finns: returnera null istället för att krascha på Average()
            return query.Any() ? query.Average() : null;
        }

        public static IReadOnlyList<DagligtResultat> SorteraDagarEfterMedelTemp(IEnumerable<VaderData> data, string plats, bool varmastForst)
        {
            // 1) Filtrera på plats
            // 2) Gruppera per dag
            // 3) Räkna dygnsmedeltemperatur
            var perDag = data
                .Where(v => v.Plats == plats)
                .GroupBy(v => v.Datum.Date)
                .Select(g => new DagligtResultat
                {
                    Datum = g.Key,
                    Varde = g.Average(x => x.Temp)
                });

            // Sortera varmast↔kallast
            perDag = varmastForst
                ? perDag.OrderByDescending(x => x.Varde)
                : perDag.OrderBy(x => x.Varde);

            return perDag.ToList();
        }

        public static IReadOnlyList<DagligtResultat> SorteraDagarEfterMedelFukt(IEnumerable<VaderData> data, string plats, bool torrastForst)
        {
            // Dygnsmedel för luftfuktighet (%)
            var perDag = data
                .Where(v => v.Plats == plats)
                .GroupBy(v => v.Datum.Date)
                .Select(g => new DagligtResultat
                {
                    Datum = g.Key,
                    Varde = g.Average(x => x.Luftfuktighet)
                });

            // Torrast = lägst RH
            perDag = torrastForst
                ? perDag.OrderBy(x => x.Varde)
                : perDag.OrderByDescending(x => x.Varde);

            return perDag.ToList();
        }

        public static IReadOnlyList<DagligtResultat> SorteraDagarEfterMogelRisk(IEnumerable<VaderData> data, string plats, bool minstRiskForst)
        {
            // Mögelrisk: andel (%) av dagens mätningar som ligger i riskzon
            // Riskzon = RH >= kritisk RH (som beror på temperaturen)
            var perDag = data
                .Where(v => v.Plats == plats)
                .GroupBy(v => v.Datum.Date)
                .Select(g =>
                {
                    var total = g.Count();

                    // Räkna hur många mätpunkter som hamnar över gränskurvan
                    var over = g.Count(m => m.Luftfuktighet >= MogelRisk.KritiskRelativFuktighet(m.Temp));

                    // Andel i procent av dagen
                    var andel = total == 0 ? 0.0 : (over * 100.0 / total);

                    return new DagligtResultat
                    {
                        Datum = g.Key,
                        Varde = andel
                    };
                });

            // Sortera minst↔mest risk
            perDag = minstRiskForst
                ? perDag.OrderBy(x => x.Varde)
                : perDag.OrderByDescending(x => x.Varde);

            return perDag.ToList();
        }

        public static IReadOnlyList<DagligtResultat> SorteraDagarEfterInneUteSkillnad(IEnumerable<VaderData> data, bool mestForst)
        {
            // VG: Vi parar ihop inne och ute på exakt samma tidsstämpel (minut för minut)
            var inne = data.Where(x => x.Plats == "Inne");
            var ute = data.Where(x => x.Plats == "Ute");

            // Join på Datum => ger diff per minut där båda mätningarna finns
            var parade = from i in inne
                         join u in ute on i.Datum equals u.Datum
                         select new { i.Datum, Diff = Math.Abs(i.Temp - u.Temp) };

            // Räkna dygnsmedel av diffen
            var perDag = parade
                .GroupBy(x => x.Datum.Date)
                .Select(g => new DagligtResultat
                {
                    Datum = g.Key,
                    Varde = g.Average(x => x.Diff)
                });

            // Sortera mest↔minst skillnad
            perDag = mestForst
                ? perDag.OrderByDescending(x => x.Varde)
                : perDag.OrderBy(x => x.Varde);

            return perDag.ToList();
        }

        public static IReadOnlyList<BalkongDorrResultat> SorteraDagarEfterBalkongDorrOppetTid(IEnumerable<VaderData> data)
        {
            // VG: Heuristik i egen klass som uppskattar "öppen"-tillstånd och summerar tid per dag
            return BalkongDorrAnalys.BeraknaOppetTidPerDag(data)
                .OrderByDescending(x => x.OppetTid)
                .ToList();
        }
    }
}
