using System;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Réglages » (spec v4 §6.3) : deux interrupteurs, Musique et Effets sonores.
    /// Simple objet piloté par PanelsController (pas un MonoBehaviour). La logique reste
    /// SoundSettings (clés son_musique, son_effets) : la page affiche et transmet les clics.
    /// Remplace les anciens boutons « Musique : oui / Effets : oui » (SoundToggleButtons).
    /// </summary>
    public class SettingsPage : IDisposable
    {
        readonly Button musicSwitch, effectsSwitch;
        readonly Label musicText, effectsText;

        public SettingsPage(VisualElement page)
        {
            musicSwitch = page.Q<Button>("toggle-musique");
            effectsSwitch = page.Q<Button>("toggle-effets");
            musicText = page.Q<Label>("toggle-musique-text");
            effectsText = page.Q<Label>("toggle-effets-text");
            // Ces interrupteurs jouent eux-mêmes leur clic (voir OnEffects).
            musicSwitch.AddToClassList(UiSounds.SilentClass);
            effectsSwitch.AddToClassList(UiSounds.SilentClass);
            musicSwitch.clicked += OnMusic;
            effectsSwitch.clicked += OnEffects;
            SoundSettings.Changed += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            musicSwitch.clicked -= OnMusic;
            effectsSwitch.clicked -= OnEffects;
            SoundSettings.Changed -= Refresh;
        }

        void OnMusic()
        {
            SoundSettings.ToggleMusic();
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiClic);
        }

        void OnEffects()
        {
            SoundSettings.ToggleEffects();
            // Quand on RÉACTIVE les effets, on entend le clic : ça confirme que ça remarche.
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiClic);
        }

        public void Refresh()
        {
            Set(musicSwitch, musicText, SoundSettings.MusicOn);
            Set(effectsSwitch, effectsText, SoundSettings.EffectsOn);
        }

        /// « OUI » : fond vert, rond à droite ; « NON » : fond gris, rond à gauche (le rond
        /// glisse grâce à la transition du USS). Jamais la couleur seule : le mot est écrit.
        static void Set(Button button, Label text, bool on)
        {
            text.text = on ? "OUI" : "NON";
            ShopUi.SetClass(button, "switch--off", !on);
        }

        /// Pour les tests : le texte affiché sur l'interrupteur de la musique.
        public string MusicText => musicText.text;
        public void DebugClickMusic() => OnMusic();
    }
}
