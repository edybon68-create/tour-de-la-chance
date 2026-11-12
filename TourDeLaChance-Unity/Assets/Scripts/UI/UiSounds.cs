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
        /// v5 : les boutons ronds de colonne jouent « ui_bouton_rond » au lieu de « ui_clic ».
        public const string RoundClass = "son-rond";
        /// v5 : les onglets jouent « ui_onglet ».
        public const string TabClass = "son-onglet";

        public static void Register(VisualElement root)
        {
            root.RegisterCallback<PointerOverEvent>(OnPointerOver);
            root.RegisterCallback<ClickEvent>(OnClick);
            root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        public static void Unregister(VisualElement root)
        {
            root.UnregisterCallback<PointerOverEvent>(OnPointerOver);
            root.UnregisterCallback<ClickEvent>(OnClick);
            root.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        }

        /// v5 : un clic sur un bouton GRISÉ joue « ui_refus » (un « bonk-bonk » gentil).
        static void OnPointerDown(PointerDownEvent evt)
        {
            for (var e = evt.target as VisualElement; e != null; e = e.parent)
            {
                if (!(e is Button b)) continue;
                if (!b.enabledInHierarchy && !b.ClassListContains(SilentClass)) PlayRefus();
                return;
            }
        }

        /// Le son de refus (bouton grisé, « Bientôt disponible », cadenas).
        public static void PlayRefus()
        {
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiRefus);
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
            if (SoundManager.Instance == null) return;
            Sfx sfx = button.ClassListContains(RoundClass) ? Sfx.UiBoutonRond
                    : button.ClassListContains(TabClass) ? Sfx.UiOnglet : Sfx.UiClic;
            SoundManager.Instance.Play(sfx);
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
