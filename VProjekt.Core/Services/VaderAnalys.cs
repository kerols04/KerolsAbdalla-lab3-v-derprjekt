using System;
using System.Collections.Generic;
using System.Linq;
using VaderProjekt.Core.Entities;
using VaderProjekt.Core.Models;

namespace VaderProjekt.Core.Services
{
    /// <summary>
    /// All beräkningslogik ligger här (Core) enligt kravbilden.
    /// UI ska bara anropa dessa metoder och skriva ut resultat.
    /// </summary>
    public static class VaderAnalys
    {
        public static double? MedelTemperaturForDatum(IEnumerable<VaderData> data, DateTime datum, string plats)
        {
            var query = data
                .Where(v => v.Plats == plats && v.Datum.Date == datum.Date)
                .Select(v => (double?)v.Temp);

            return query.Any() ? query.Average() : null;
        }

        public static IReadOnlyList<DagligtResultat> SorteraDagarEfterMedelTemp(IEnumerable<VaderData> data, string plats, bool varmastForst)
        {
            var sorterad = data
                .Where(v => v.Plats == plats)
                .GroupBy(v => v.Datum.Date)
                .Select(g => new DagligtResultat
                {
                    Datum = g.Key,
                    Varde = Math.Round(g.Average(x => x.Temp), 2)
                });

            sorterad = varmastForst
                ? sorterad.OrderByDescending(x => x.Varde)
                : sorterad.OrderBy(x => x.Varde);

            return sorterad.ToList();
        }

        public static IReadOnlyList<DagligtResultat> SorteraDagarEfterMedelFukt(IEnumerable<VaderData> data, string plats, bool torrastForst)
        {
            var sorterad = data
                .Where(v => v.Plats == plats)
                .GroupBy(v => v.Datum.Date)
                .Select(g => new DagligtResultat
                {
                    Datum = g.Key,
                    Varde = Math.Round(g.Average(x => x.Luftfuktighet), 2)
                });

            sorterad = torrastForst
                ? sorterad.OrderBy(x => x.Varde)
                : sorterad.OrderByDescending(x => x.Varde);

            return sorterad.ToList();
        }

        /// <summary>
        /// Mögelrisk: vi räknar "andel av dagens minuter" där RH ligger över en temperaturberoende riskkurva.
        /// Uppgiften poängterar att korrektheten inte är avgörande, men att beräkningen ska ge olika resultat
        /// beroende på temp + fukt.
        /// </summary>
        public static IReadOnlyList<DagligtResultat> SorteraDagarEfterMogelRisk(IEnumerable<VaderData> data, string plats, bool minstRiskForst)
        {
            var perDag = data
                .Where(v => v.Plats == plats)
                .GroupBy(v => v.Datum.Date)
                .Select(g =>
                {
                    var total = g.Count();
                    var over = g.Count(m => m.Luftfuktighet >= MogelRisk.KritiskRelativFuktighet(m.Temp));
                    var andel = total == 0 ? 0.0 : (over * 100.0 / total);

                    return new DagligtResultat
                    {
                        Datum = g.Key,
                        Varde = Math.Round(andel, 1) // % av dagens mätpunkter i riskzon
                    };
                });

            perDag = minstRiskForst
                ? perDag.OrderBy(x => x.Varde)
                : perDag.OrderByDescending(x => x.Varde);

            return perDag.ToList();
        }

        /// <summary>
        /// VG: Sortering på dagar då inne/ute skiljer sig mest/minst.
        /// Vi matchar mätningar på exakt tidstämpel och tar dagligt MEDEL av absolut skillnad.
        /// </summary>
        public static IReadOnlyList<DagligtResultat> SorteraDagarEfterInneUteSkillnad(IEnumerable<VaderData> data, bool mestForst)
        {
            var inne = data.Where(x => x.Plats == "Inne");
            var ute = data.Where(x => x.Plats == "Ute");

            // Join på exakt tidstämpel: ger par av (inne, ute) för varje minut som finns i båda serier.
            var parade = from i in inne
                         join u in ute on i.Datum equals u.Datum
                         select new { i.Datum, Diff = Math.Abs(i.Temp - u.Temp) };

            var perDag = parade
                .GroupBy(x => x.Datum.Date)
                .Select(g => new DagligtResultat
                {
                    Datum = g.Key,
                    Varde = Math.Round(g.Average(x => x.Diff), 2)
                });

            perDag = mestForst
                ? perDag.OrderByDescending(x => x.Varde)
                : perDag.OrderBy(x => x.Varde);

            return perDag.ToList();
        }

        /// <summary>
        /// VG: Uppskatta hur länge balkongdörren är öppen per dag.
        /// Heuristik:
        /// - När dörren öppnas: inne-temp går snabbt NED och ute-temp går snabbt UPP (termometern sitter nära dörren).
        /// - Vi räknar minuter i ett "öppen"-tillstånd och summerar per dag.
        /// </summary>
        public static IReadOnlyList<BalkongDorrResultat> SorteraDagarEfterBalkongDorrOppetTid(IEnumerable<VaderData> data)
        {
            return BalkongDorrAnalys.BeraknaOppetTidPerDag(data)
                .OrderByDescending(x => x.OppetTid)
                .ToList();
        }
    }
}
