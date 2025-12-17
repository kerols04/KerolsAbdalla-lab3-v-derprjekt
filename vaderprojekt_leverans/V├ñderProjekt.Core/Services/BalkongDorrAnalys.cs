using System;
using System.Collections.Generic;
using System.Linq;
using VaderProjekt.Core.Entities;
using VaderProjekt.Core.Models;

namespace VaderProjekt.Core.Services
{
    public static class BalkongDorrAnalys
    {
        // Trösklar: justerbara heuristik-parametrar.
        private const double OppnaTraskelInneFall = 0.3; // °C/min
        private const double OppnaTraskelUteStig = 0.3;  // °C/min

        private const double StangTraskelInneStig = 0.2; // °C/min
        private const double StangTraskelUteFall = 0.2;  // °C/min

        public static IReadOnlyList<BalkongDorrResultat> BeraknaOppetTidPerDag(IEnumerable<VaderData> data)
        {
            var inne = data.Where(x => x.Plats == "Inne");
            var ute = data.Where(x => x.Plats == "Ute");

            // Matcha mätningar på tidstämpel
            var parade = (from i in inne
                          join u in ute on i.Datum equals u.Datum
                          select new { Tid = i.Datum, Inne = i.Temp, Ute = u.Temp })
                          .OrderBy(x => x.Tid)
                          .ToList();

            // Grupp per dag
            var perDag = parade.GroupBy(x => x.Tid.Date);
            var resultat = new List<BalkongDorrResultat>();

            foreach (var dag in perDag)
            {
                var lista = dag.OrderBy(x => x.Tid).ToList();
                if (lista.Count < 2)
                {
                    resultat.Add(new BalkongDorrResultat { Datum = dag.Key, OppetTid = TimeSpan.Zero });
                    continue;
                }

                bool dorrOppet = false;
                double oppnaMinuter = 0;

                for (int i = 1; i < lista.Count; i++)
                {
                    var prev = lista[i - 1];
                    var curr = lista[i];

                    // Beräkna tidssteg (vi vill inte råka lägga på jättestora luckor som "öppen tid")
                    var dt = curr.Tid - prev.Tid;
                    var minuter = Math.Clamp(dt.TotalMinutes, 0, 5); // max 5 min per steg för säkerhet

                    var dInne = curr.Inne - prev.Inne;
                    var dUte = curr.Ute - prev.Ute;

                    bool oppnaSignal = (dInne <= -OppnaTraskelInneFall) && (dUte >= OppnaTraskelUteStig);
                    bool stangSignal = (dorrOppet && (dInne >= StangTraskelInneStig) && (dUte <= -StangTraskelUteFall));

                    if (!dorrOppet)
                    {
                        if (oppnaSignal)
                        {
                            dorrOppet = true;
                            oppnaMinuter += minuter;
                        }
                    }
                    else
                    {
                        oppnaMinuter += minuter;
                        if (stangSignal)
                            dorrOppet = false;
                    }
                }

                resultat.Add(new BalkongDorrResultat
                {
                    Datum = dag.Key,
                    OppetTid = TimeSpan.FromMinutes(oppnaMinuter)
                });
            }

            return resultat;
        }
    }
}
