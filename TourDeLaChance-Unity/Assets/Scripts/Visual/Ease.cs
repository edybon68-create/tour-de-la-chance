using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les courbes d'animation de la spec (sons-animations §3). t va de 0 (début) à 1 (fin)
    /// et la courbe dit « où on en est » : une animation linéaire paraît mécanique,
    /// une courbe la rend vivante.
    /// </summary>
    public static class Ease
    {
        /// Démarre vite, finit en douceur : 1 − (1 − t)³
        public static float Out(float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// Démarre doucement, finit vite : t³
        public static float In(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        /// Doux au début et à la fin.
        public static float InOut(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.5f) return 4f * t * t * t;
            float u = -2f * t + 2f;
            return 1f - u * u * u / 2f;
        }

        /// Monte puis redescend (0 → 1 → 0) : idéal pour un « pop » ou un saut.
        public static float Sine(float t) => Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

        /// « Pop » d'apparition : 0,6 → 1,08 → 1,00 (utilisé pour les boutons).
        /// De 0 à 70 % du temps on grossit jusqu'à `overshoot`, puis on revient à 1.
        public static float Pop(float t, float from, float overshoot)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.7f) return Mathf.Lerp(from, overshoot, Out(t / 0.7f));
            return Mathf.Lerp(overshoot, 1f, InOut((t - 0.7f) / 0.3f));
        }
    }
}
