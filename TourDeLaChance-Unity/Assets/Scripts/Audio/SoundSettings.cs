using System;
using UnityEngine;

namespace TourDeLaChance
{
    /// <summary>
    /// Les réglages du son : musique oui/non, effets oui/non (spec §6.2 et §7).
    /// Classe "static" sur le modèle de Wallet, sauvegardée dans les PlayerPrefs.
    ///
    /// Attention au nom : "AudioSettings" existe déjà dans Unity (UnityEngine.AudioSettings),
    /// d'où "SoundSettings". Ces réglages ne touchent à rien d'autre (règles, gains, piège).
    /// </summary>
    public static class SoundSettings
    {
        public const string MusicKey = "son_musique";
        public const string EffectsKey = "son_effets";

        /// Prévient le lecteur de musique, le gestionnaire d'effets et les boutons.
        public static event Action Changed;

        public static bool MusicOn => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        public static bool EffectsOn => PlayerPrefs.GetInt(EffectsKey, 1) == 1;

        public static void SetMusic(bool on)
        {
            PlayerPrefs.SetInt(MusicKey, on ? 1 : 0);
            PlayerPrefs.Save(); // sauvegarde à chaque clic (§7)
            Changed?.Invoke();
        }

        public static void SetEffects(bool on)
        {
            PlayerPrefs.SetInt(EffectsKey, on ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void ToggleMusic() => SetMusic(!MusicOn);
        public static void ToggleEffects() => SetEffects(!EffectsOn);
    }
}
