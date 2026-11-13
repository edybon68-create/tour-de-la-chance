using System;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// La pop-up « Réglages » (spec v4 §6.3) : deux interrupteurs, Musique et Effets sonores.
    /// Simple objet piloté par PanelsController (pas un MonoBehaviour). La logique reste
    /// SoundSettings (clés son_musique, son_effets) : la page affiche et transmet les clics.
    /// Remplace les anciens boutons « Musique : oui / Effets : oui » (SoundToggleButtons).
    ///
    /// v6 §12 : une 3e ligne « Graphismes » : 3 segments Faible · Moyen · Élevé (celui choisi est
    /// allumé, en or) et une phrase qui change avec le choix. La règle est dans GraphicsLevel ;
    /// le rendu est changé tout de suite par GraphicsApplier.
    /// </summary>
    public class SettingsPage : IDisposable
    {
        readonly Button musicSwitch, effectsSwitch;
        readonly Label musicText, effectsText;
        readonly Button[] segments = new Button[3];
        readonly Label graphPhrase;
        static readonly string[] Phrases =
        {
            "Pour les ordinateurs peu puissants : plus fluide, moins de détails.",
            "L'équilibre conseillé.",
            "Tous les détails, pour les ordinateurs puissants."
        };
        /// Le petit message « Graphismes : Élevé » (posé par PanelsController).
        public event Action<string> Toast;

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

            // v6 §12 : la ligne Graphismes, construite ici (icône, titre, sélecteur, phrase).
            var graph = page.Q<VisualElement>("settings-graph");
            graph.Clear();
            var titleRow = new VisualElement { pickingMode = PickingMode.Ignore };
            titleRow.AddToClassList("settings-graph__titre-rangee");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("settings-graph__icone");
            if (UiImages.Graphismes != null) icon.style.backgroundImage = new StyleBackground(UiImages.Graphismes);
            titleRow.Add(icon);
            titleRow.Add(ShopUi.NewLabel("Graphismes", "settings-graph__titre"));
            graph.Add(titleRow);
            var selector = new VisualElement();
            selector.AddToClassList("selecteur");
            for (int i = 0; i < 3; i++)
            {
                int level = i;
                var b = new Button(() => OnGraphics(level));
                b.AddToClassList("btn"); b.AddToClassList("bulle"); b.AddToClassList("segment"); b.AddToClassList(UiSounds.TabClass);
                // Les 3 petites barres montantes : autant de pleines que le niveau.
                var bars = new VisualElement { pickingMode = PickingMode.Ignore };
                bars.AddToClassList("barres");
                for (int k = 0; k < 3; k++)
                {
                    var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                    bar.AddToClassList("barre");
                    if (k <= i) bar.AddToClassList("barre--pleine");
                    bar.style.height = 14 + 8 * k;
                    bars.Add(bar);
                }
                b.Add(bars);
                b.Add(ShopUi.NewLabel(GraphicsLevel.Names[i], "segment__texte"));
                segments[i] = b;
                selector.Add(b);
            }
            graph.Add(selector);
            graphPhrase = ShopUi.NewLabel("", "settings-graph__phrase");
            graph.Add(graphPhrase);
            GraphicsLevel.Changed += Refresh;
            Refresh();
        }

        void OnGraphics(int level)
        {
            if (level == GraphicsLevel.Current) return;
            GraphicsLevel.Set(level); // sauvegardé ; GraphicsApplier change le rendu tout de suite
            Toast?.Invoke($"Graphismes : {GraphicsLevel.Names[level]}");
        }

        /// OUTIL DE TEST : clic sur un segment (0 Faible, 1 Moyen, 2 Élevé).
        public void DebugClickGraphics(int level) => OnGraphics(level);

        public void Dispose()
        {
            musicSwitch.clicked -= OnMusic;
            effectsSwitch.clicked -= OnEffects;
            SoundSettings.Changed -= Refresh;
            GraphicsLevel.Changed -= Refresh;
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
            int g = GraphicsLevel.Current;
            for (int i = 0; i < segments.Length; i++) if (segments[i] != null) ShopUi.SetClass(segments[i], "segment--choisi", i == g);
            if (graphPhrase != null) graphPhrase.text = Phrases[g];
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
