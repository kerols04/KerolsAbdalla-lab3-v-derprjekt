using System;
using System.Collections.Generic;
using System.Linq;
using VaderProjekt.Core.Entities;
using VaderProjekt.Core.Models;

namespace VaderProjekt.Core.Services
{
    /// <summary>
    /// VG: Uppskattar hur länge balkongdörren varit öppen per dag.
    /// Antagande från uppgiften: när dörren öppnas sjunker innertemp snabbt samtidigt som utetemp stiger snabbt.
    /// Vi använder en enkel heuristik med trösklar och summerar tid i "öppen"-läge per dag.
    /// </summary>
    public static class BalkongDorrAnalys
    {
        // Trösklar (°C per steg). Dessa är heuristiska och kan justeras vid behov.
        private const double OppnaTraskelInneFall = 0.3; // inne faller minst 0.3°C
        private const double OppnaTraskelUteStig  = 0.3; // ute stiger minst 0.3°C

        private const double StangTraskelInneStig = 0.2; // inne börjar stiga igen
        private const double StangTraskelUteFall  = 0.2; // ute börjar falla igen

        public static IReadOnlyList<BalkongDorrResultat> BeraknaOppetTidPerDag(IEnumerable<VaderData> data)
        {
            // Dela upp datan i inne/ute
            var inne = data.Where(x => x.Plats == "Inne");
            var ute  = data.Where(x => x.Plats == "Ute");

            // Para ihop mätningar som har exakt samma tidsstämpel (minut för minut).
            // Detta ger oss (Tid, InneTemp, UteTemp) för alla minuter där båda mätningarna finns.
            var parade = (from i in inne
                          join u in ute on i.Datum equals u.Datum
                          select new { Tid = i.Datum, Inne = i.Temp, Ute = u.Temp })
                         .OrderBy(x => x.Tid)
                         .ToList();

            // Gruppera per dag
            var perDag = parade.GroupBy(x => x.Tid.Date);
            var resultat = new List<BalkongDorrResultat>();

            foreach (var dag in perDag)
            {
                var lista = dag.OrderBy(x => x.Tid).ToList();

                // Om vi har för få punkter: ingen meningsfull analys
                if (lista.Count < 2)
                {
                    resultat.Add(new BalkongDorrResultat { Datum = dag.Key, OppetTid = TimeSpan.Zero });
                    continue;
                }

                bool dorrOppet = false;
                double oppnaMinuter = 0;

                // Gå igenom mätningar i tidsordning och kolla förändring mellan två intilliggande punkter
                for (int i = 1; i < lista.Count; i++)
                {
                    var prev = lista[i - 1];
                    var curr = lista[i];

                    // Tidssteg: vi vill inte räkna jätteluckor som "öppen tid"
                    var dt = curr.Tid - prev.Tid;
                    var minuter = Math.Clamp(dt.TotalMinutes, 0, 5); // max 5 min per steg

                    // Temperaturförändring från föregående mätning
                    var dInne = curr.Inne - prev.Inne;
                    var dUte  = curr.Ute  - prev.Ute;

                    // Öppningssignal: inne ner och ute upp
                    bool oppnaSignal =
                        (dInne <= -OppnaTraskelInneFall) &&
                        (dUte  >=  OppnaTraskelUteStig);

                    // Stängningssignal: när vi är öppna och inne börjar stiga samtidigt som ute börjar falla
                    bool stangSignal =
                        dorrOppet &&
                        (dInne >=  StangTraskelInneStig) &&
                        (dUte  <= -StangTraskelUteFall);

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
                        // När dörren är öppen räknar vi tiden tills vi ser stängsignal
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

