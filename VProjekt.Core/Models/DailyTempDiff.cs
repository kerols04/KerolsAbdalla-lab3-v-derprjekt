using System;

namespace VaderProjekt.Core.Models
{
    public sealed class DailyTempDiff
    {
        public DailyTempDiff(DateTime datum, double medelAbsolutSkillnad)
        {
            Datum = datum.Date;
            MedelAbsolutSkillnad = medelAbsolutSkillnad;
        }

        public DateTime Datum { get; }

        /// <summary>
        /// Medel av |InneTemp - UteTemp| över dagen (parat på samma tidsstämplar).
        /// </summary>
        public double MedelAbsolutSkillnad { get; }
    }
}
