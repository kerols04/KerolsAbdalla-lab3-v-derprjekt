using System;

namespace VaderProjekt.Core.Services
{
    /// <summary>
    /// Hjälpmetoder för mögelrisk.
    /// Vi använder en temperaturberoende “kritisk RH”-kurva (förenklad polynom-approximation).
    /// Kravet är att risken ska bero på både temperatur och luftfuktighet, inte att formeln är exakt.
    /// </summary>
    public static class MogelRisk
    {
        /// <summary>
        /// Returnerar kritisk relativ luftfuktighet (%) vid given temperatur (°C).
        /// Om faktisk RH ligger över denna gräns under en mätpunkt räknar vi det som "riskzon".
        /// Vi klampar temp till 0–30°C (där kurvan normalt används) och RH till 0–100%.
        /// </summary>
        public static double KritiskRelativFuktighet(double tempC)
        {
            // Begränsa temperatur till intervallet där formeln är rimlig
            var x = Math.Clamp(tempC, 0.0, 30.0);

            // Polynomkurva för kritisk RH (vanligt förekommande approximationskurva)
            // y = (5/100000)*x^4 - 0.0045*x^3 + 0.1652*x^2 - 2.9381*x + 97
            var y = (5.0 / 100000.0) * Math.Pow(x, 4)
                    - 0.0045 * Math.Pow(x, 3)
                    + 0.1652 * Math.Pow(x, 2)
                    - 2.9381 * x
                    + 97.0;

            // Säkerhet: RH kan aldrig vara under 0 eller över 100
            return Math.Clamp(y, 0.0, 100.0);
        }
    }
}
