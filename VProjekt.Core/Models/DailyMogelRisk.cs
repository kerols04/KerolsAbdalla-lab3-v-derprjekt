using System;

namespace VaderProjekt.Core.Models
{
    public sealed class DailyMogelRisk
    {
        public DailyMogelRisk(DateTime datum, double riskProcent)
        {
            Datum = datum.Date;
            RiskProcent = riskProcent;
        }

        public DateTime Datum { get; }

        /// <summary>
        /// Andel av dagens mätningar där fuktigheten ligger över en temperaturberoende tröskel.
        /// 0-100 (%).
        /// </summary>
        public double RiskProcent { get; }
    }
}
