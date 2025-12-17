using System;

namespace VaderProjekt.Core.Models
{
    public sealed class DailyDoorOpen
    {
        public DailyDoorOpen(DateTime datum, TimeSpan oppenTid)
        {
            Datum = datum.Date;
            OppenTid = oppenTid;
        }

        public DateTime Datum { get; }

        /// <summary>
        /// Uppskattad total tid då balkongdörren varit öppen (summerad per dag).
        /// </summary>
        public TimeSpan OppenTid { get; }
    }
}
