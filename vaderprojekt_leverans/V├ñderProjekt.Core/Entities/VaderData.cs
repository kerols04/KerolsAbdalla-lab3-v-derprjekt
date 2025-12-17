using System;
using System.ComponentModel.DataAnnotations;

namespace VaderProjekt.Core.Entities
{
    /// <summary>
    /// En rad i databasen: en mätning av temperatur och luftfuktighet vid en viss tidpunkt.
    /// Datamodellen behöver inte normaliseras i denna uppgift, därför räcker en enda tabell.
    /// </summary>
    public class VaderData
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Datum och tid från filen (t.ex. 2016-10-01 0:00).
        /// </summary>
        public DateTime Datum { get; set; }

        /// <summary>
        /// "Ute" eller "Inne".
        /// </summary>
        public string Plats { get; set; } = string.Empty;

        /// <summary>
        /// Temperatur i grader Celsius.
        /// CSV-filen använder punkt som decimaltecken ("10.3").
        /// </summary>
        public double Temp { get; set; }

        /// <summary>
        /// Relativ luftfuktighet i procent (0-100).
        /// </summary>
        public int Luftfuktighet { get; set; }
    }
}
