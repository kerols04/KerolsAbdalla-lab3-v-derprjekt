using System;

namespace VaderProjekt.Core.Models
{
    /// <summary>
    /// Resultat för balkongdörr-analysen:
    /// hur länge dörren uppskattas ha varit öppen en viss dag.
    /// </summary>
    public sealed class BalkongDorrResultat
    {
        // Dagen som resultatet gäller.
        public DateTime Datum { get; init; }

        // Summerad “öppen tid” under dagen (uppskattning).
        public TimeSpan OppetTid { get; init; }

        // Gör listan lättläst i konsolen.
        public override string ToString()
        {
            var timmar = (int)OppetTid.TotalHours;
            var minuter = OppetTid.Minutes;
            return $"{Datum:yyyy-MM-dd}: {timmar:D2}h {minuter:D2}m";
        }
    }
}
