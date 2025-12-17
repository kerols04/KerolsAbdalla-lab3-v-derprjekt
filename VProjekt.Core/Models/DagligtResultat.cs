using System;

namespace VaderProjekt.Core.Models
{
    /// <summary>
    /// Enkel DTO för att presentera dagliga resultat i UI.
    /// </summary>
    public sealed class DagligtResultat
    {
        public DateTime Datum { get; init; }
        public double Varde { get; init; }

        public override string ToString() => $"{Datum:yyyy-MM-dd}: {Varde:F1}";
    }
}
