using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Un compteur qui « défile » (spec §3.8) : la valeur RÉELLE change d'un coup (c'est la
    /// logique qui décide), et la valeur AFFICHÉE lui court après pendant quelques dixièmes
    /// de seconde. Petite classe ordinaire (pas un MonoBehaviour) : HudController en a deux.
    /// </summary>
    public class RollingCounter
    {
        float from, to, elapsed, duration;
        Func<float, float> ease;

        /// La valeur affichée en ce moment (nombre entier, arrondi).
        public int Displayed { get; private set; }
        /// La valeur visée (la vraie valeur).
        public int Target { get; private set; }
        /// Un défilement est-il en cours ?
        public bool Rolling { get; private set; }
        /// Où en est le défilement (0 → 1).
        public float Progress => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

        /// Affiche directement une valeur, sans défilement.
        public void Snap(int value)
        {
            Displayed = Target = value;
            Rolling = false;
        }

        /// Lance un défilement de la valeur affichée actuelle vers "value".
        public void RollTo(int value, float seconds, Func<float, float> curve)
        {
            from = Displayed;
            to = Target = value;
            elapsed = 0f;
            duration = seconds;
            ease = curve;
            Rolling = true;
        }

        /// À appeler à chaque image. À la fin, la valeur affichée est EXACTEMENT la cible.
        public void Tick(float deltaTime)
        {
            if (!Rolling) return;
            elapsed += deltaTime;
            if (elapsed >= duration)
            {
                Displayed = Target;
                Rolling = false;
                return;
            }
            Displayed = Mathf.RoundToInt(Mathf.Lerp(from, to, ease(elapsed / duration)));
        }
    }
}
