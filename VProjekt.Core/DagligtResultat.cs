using System;

namespace VaderProjekt.Core.Models
{
    /// <summary>
    /// En liten “resultat-rad” som UI kan skriva ut.
    /// Ex: datum + ett uträknat värde (medeltemp, medelfukt, mögelrisk osv).
    /// </summary>
    public sealed class DagligtResultat
    {
        // Själva dagen (utan tid).
        public DateTime Datum { get; init; }

        // Det beräknade värdet för dagen.
        public double Varde { get; init; }

        // Gör utskrift enkel och konsekvent i UI.
        public override string ToString() => $"{Datum:yyyy-MM-dd}: {Varde:F1}";
    }
}
