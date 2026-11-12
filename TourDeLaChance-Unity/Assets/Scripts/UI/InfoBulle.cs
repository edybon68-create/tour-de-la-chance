using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'INFO-BULLE maison (spec v5 §7.1, art bible §12.17.4). Les boutons de colonne n'ont plus
    /// de libellé : au survol (après 0,4 s), une petite bulle affiche leur nom à côté du bouton,
    /// vers l'intérieur de l'écran. Sur un bouton grisé, elle explique pourquoi :
    /// « Boutique (pas pendant une épreuve) ». Elle disparaît dès que la souris sort.
    ///
    /// Pourquoi « maison » ? La propriété « tooltip » d'UI Toolkit ne marche que dans l'éditeur.
    /// Un seul élément sert à tous les boutons : le code le déplace et change son texte.
    ///
    /// Astuce : un bouton grisé ne reçoit plus les événements de la souris ; on écoute donc son
    /// PARENT (le « btn-holder », de la même taille), qui lui reste actif.
    /// Ce n'est pas un MonoBehaviour : PanelsController le crée et l'avance à chaque image.
    /// </summary>
    public class InfoBulle : IDisposable
    {
        public enum Side { Right, Left, Below }

        const float Delay = 0.4f;   // temps de survol avant l'apparition
        const float Gap = 14f;      // écart entre le bouton et la bulle

        class Target
        {
            public Button button;
            public VisualElement holder;
            public string name, reason;
            public Side side;
            public Func<bool> allowed;
            public EventCallback<PointerEnterEvent> enter;
            public EventCallback<PointerLeaveEvent> leave;
            public EventCallback<PointerDownEvent> down;
        }

        readonly VisualElement bubble;
        readonly Label label;
        readonly List<Target> targets = new List<Target>();
        Target hovered;
        float hoverTime;
        bool shown;

        /// Le texte affiché en ce moment ("" si la bulle est cachée), pour les tests.
        public string Text => shown ? label.text : "";

        public InfoBulle(VisualElement bubble, Label label)
        {
            this.bubble = bubble;
            this.label = label;
            bubble.pickingMode = PickingMode.Ignore;
            label.pickingMode = PickingMode.Ignore;
            Hide();
        }

        /// Branche l'info-bulle sur un bouton : son nom, de quel côté s'afficher, et quand il
        /// est permis (sinon la bulle ajoute la raison entre parenthèses).
        public void Attach(Button button, string name, Side side, Func<bool> allowed, string reason)
        {
            var holder = button.parent ?? button;
            holder.pickingMode = PickingMode.Position;
            var t = new Target { button = button, holder = holder, name = name, side = side, allowed = allowed, reason = reason };
            t.enter = _ => { hovered = t; hoverTime = 0f; };
            t.leave = _ => { if (hovered == t) { hovered = null; Hide(); } };
            t.down = _ => Hide(); // au clic, la bulle s'efface
            holder.RegisterCallback(t.enter);
            holder.RegisterCallback(t.leave);
            holder.RegisterCallback(t.down, TrickleDown.TrickleDown);
            targets.Add(t);
        }

        public void Dispose()
        {
            foreach (var t in targets)
            {
                t.holder.UnregisterCallback(t.enter);
                t.holder.UnregisterCallback(t.leave);
                t.holder.UnregisterCallback(t.down, TrickleDown.TrickleDown);
            }
            targets.Clear();
        }

        /// Avance le temps de survol (appelé à chaque image).
        public void Tick(float dt)
        {
            if (hovered == null) return;
            // Bouton caché entre-temps (colonnes repliées) : on oublie le survol.
            if (hovered.button.resolvedStyle.display == DisplayStyle.None || hovered.holder.panel == null)
            {
                hovered = null;
                Hide();
                return;
            }
            hoverTime += dt;
            if (!shown && hoverTime >= Delay) Show(hovered);
            else if (shown) Place(hovered); // il suit le bouton (colonnes qui glissent)
        }

        void Show(Target t)
        {
            bool ok = t.allowed == null || t.allowed();
            label.text = ok ? t.name : $"{t.name} ({t.reason})";
            shown = true;
            bubble.style.display = DisplayStyle.Flex;
            bubble.AddToClassList("info-bulle--visible");
            // La bordure haute reprend la couleur « anneau » du bouton (rappel du code couleur).
            var ring = t.button.Q<VisualElement>(className: "col-btn-anneau");
            if (ring != null) bubble.style.borderTopColor = ring.resolvedStyle.borderTopColor;
            Place(t);
            if (SoundManager.Instance != null) SoundManager.Instance.Play(Sfx.UiInfobulle);
        }

        /// Place la bulle à côté du bouton, centrée sur lui, sans sortir de l'écran.
        void Place(Target t)
        {
            var parent = bubble.parent;
            if (parent == null) return;
            Rect b = parent.WorldToLocal(t.button.worldBound);
            float w = bubble.resolvedStyle.width, h = bubble.resolvedStyle.height;
            if (float.IsNaN(w) || w <= 0f) w = 200f;
            if (float.IsNaN(h) || h <= 0f) h = 52f;
            float x, y;
            switch (t.side)
            {
                case Side.Left: x = b.xMin - Gap - w; y = b.center.y - h * 0.5f; break;
                case Side.Below: x = b.center.x - w * 0.5f; y = b.yMax + Gap; break;
                default: x = b.xMax + Gap; y = b.center.y - h * 0.5f; break;
            }
            float maxX = parent.layout.width - w - 8f;
            bubble.style.left = Mathf.Clamp(x, 8f, Mathf.Max(8f, maxX));
            bubble.style.top = Mathf.Max(8f, y);
        }

        void Hide()
        {
            shown = false;
            bubble.RemoveFromClassList("info-bulle--visible");
            bubble.style.display = DisplayStyle.None;
        }
    }
}
