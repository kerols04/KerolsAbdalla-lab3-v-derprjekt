using System;

namespace VaderProjekt.Core.Models
{
    public sealed class DailyValue
    {
        public DailyValue(DateTime datum, double value)
        {
            Datum = datum.Date;
            Value = value;
        }

        public DateTime Datum { get; }
        public double Value { get; }
    }
}
