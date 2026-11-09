using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Les deux petits boutons « Musique : oui/non » et « Effets : oui/non » (spec §6.2).
    /// Ils existent à deux endroits (HUD et écran titre) : cette petite classe (pas un
    /// MonoBehaviour) les branche sur la même logique, SoundSettings.
    /// </summary>
    public class SoundToggleButtons
    {
        readonly Button musicButton, effectsButton;

        public SoundToggleButtons(Button music, Button effects)
        {
            musicButton = music;
            effectsButton = effects;
            // Ces boutons gèrent eux-mêmes leur son de clic (voir OnEffectsClicked).
            musicButton.AddToClassList(UiSounds.SilentClass);
            effectsButton.AddToClassList(UiSounds.SilentClass);
            musicButton.clicked += OnMusicClicked;
            effectsButton.clicked += OnEffectsClicked;
            SoundSettings.Changed += Refresh;
            Refresh();
        }

        /// À appeler quand l'écran se désactive (sinon on resterait abonné).
        public void Dispose()
        {
            musicButton.clicked -= OnMusicClicked;
            effectsButton.clicked -= OnEffectsClicked;
            SoundSettings.Changed -= Refresh;
        }

        void OnMusicClicked()
        {
            SoundSettings.ToggleMusic();
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiClic);
        }

        void OnEffectsClicked()
        {
            SoundSettings.ToggleEffects();
            // Quand on RÉACTIVE les effets, on entend le clic : ça confirme que ça remarche.
            // (Quand on les coupe, Play ne fait rien : c'est voulu.)
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiClic);
        }

        void Refresh()
        {
            Set(musicButton, "Musique", SoundSettings.MusicOn);
            Set(effectsButton, "Effets", SoundSettings.EffectsOn);
        }

        static void Set(Button button, string label, bool on)
        {
            button.text = $"{label} : {(on ? "oui" : "non")}";
            if (on) button.RemoveFromClassList("sound-toggle--off");
            else button.AddToClassList("sound-toggle--off");
        }
    }
}
