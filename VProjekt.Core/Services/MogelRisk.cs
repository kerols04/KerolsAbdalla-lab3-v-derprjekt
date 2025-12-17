using System;

namespace VaderProjekt.Core.Services
{
    /// <summary>
    /// Hjälpmetoder för mögelrisk.
    /// Vi använder en förenklad riskkurva (polynom) som ger en kritisk RH beroende på temperatur.
    /// (Enligt uppgiftens länktips, och räcker eftersom kravet är temp+fukt-beroende resultat.)
    /// </summary>
    public static class MogelRisk
    {
        /// <summary>
        /// Returnerar kritisk relativ luftfuktighet (%) vid given temperatur (°C).
        /// Temperaturen klampas till 0–30°C eftersom formeln typiskt anges för det intervallet.
        /// </summary>
        public static double KritiskRelativFuktighet(double tempC)
        {
            var x = Math.Clamp(tempC, 0.0, 30.0);

            // Polynom (en vanligt förekommande approximationskurva):
            // y = (5/100000)*x^4 - 0.0045*x^3 + 0.1652*x^2 - 2.9381*x + 97
            var y = (5.0 / 100000.0) * Math.Pow(x, 4)
                    - 0.0045 * Math.Pow(x, 3)
                    + 0.1652 * Math.Pow(x, 2)
                    - 2.9381 * x
                    + 97.0;

            // Rimlighetsklamp (0-100% RH)
            return Math.Clamp(y, 0.0, 100.0);
        }
    }
}
