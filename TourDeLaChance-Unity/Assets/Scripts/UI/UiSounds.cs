using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Les petits sons des boutons (spec §3.10) : `ui_survol` quand la souris arrive sur un
    /// bouton, `ui_clic` quand on clique. On les branche UNE fois sur la racine d'un écran :
    /// les événements « remontent » des boutons vers la racine (on dit qu'ils « bouillonnent »),
    /// donc tous les boutons, même ceux créés plus tard par le code, sont concernés.
    ///
    /// Un bouton avec la classe USS "no-ui-sound" est ignoré : il joue son propre son
    /// (ex. « Jouer » joue ui_jouer, les boutons Musique/Effets gèrent leur clic eux-mêmes).
    /// </summary>
    public static class UiSounds
    {
        public const string SilentClass = "no-ui-sound";

        public static void Register(VisualElement root)
        {
            root.RegisterCallback<PointerOverEvent>(OnPointerOver);
            root.RegisterCallback<ClickEvent>(OnClick);
        }

        public static void Unregister(VisualElement root)
        {
            root.UnregisterCallback<PointerOverEvent>(OnPointerOver);
            root.UnregisterCallback<ClickEvent>(OnClick);
        }

        static void OnPointerOver(PointerOverEvent evt)
        {
            // Les enfants de nos boutons ignorent la souris (picking-mode="Ignore") : cet
            // événement n'arrive donc qu'en ENTRANT sur le bouton, une fois par survol.
            var button = ButtonOf(evt.target as VisualElement);
            if (button == null) return;
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiSurvol);
        }

        static void OnClick(ClickEvent evt)
        {
            var button = ButtonOf(evt.target as VisualElement);
            if (button == null) return;
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiClic);
        }

        /// Le bouton qui contient cet élément (ou lui-même), s'il est actif et pas silencieux.
        static Button ButtonOf(VisualElement element)
        {
            while (element != null)
            {
                if (element is Button b)
                    return b.enabledInHierarchy && !b.ClassListContains(SilentClass) ? b : null;
                element = element.parent;
            }
            return null;
        }
    }
}
