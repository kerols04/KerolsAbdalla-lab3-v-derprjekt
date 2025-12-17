using System;

namespace VaderProjekt.Core.Models
{
    public sealed class BalkongDorrResultat
    {
        public DateTime Datum { get; init; }
        public TimeSpan OppetTid { get; init; }

        public override string ToString()
        {
            var timmar = (int)OppetTid.TotalHours;
            var minuter = OppetTid.Minutes;
            return $"{Datum:yyyy-MM-dd}: {timmar:D2}h {minuter:D2}m";
        }
    }
}
