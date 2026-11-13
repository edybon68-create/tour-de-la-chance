using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Petits outils d'AFFICHAGE pour les boutons v4 (spec v4 §5.3), sur le modèle de
    /// UiSounds.Register : on les branche UNE fois sur la racine d'un écran.
    ///
    /// - Register(root) / Apply(élément) : pour chaque bouton de classe .btn, ajoute le REFLET
    ///   (bande blanche en haut, dessinée), le REBOND au relâché (classe .btn--rebond posée
    ///   0,07 s) et, pour les gros boutons (.btn--ombre), l'OMBRE PORTÉE. Les écrans qui
    ///   recréent des cartes par le code rappellent Apply sur le contenu recréé. Il retire aussi
    ///   le focus clavier des boutons (correctif B2).
    /// - AddShine(button) : l'éclat qui passe, réservé au bouton « Jouer ».
    /// Le reste du style (contour, épaisseur, dégradé, états) est dans Assets/UI/Buttons.uss.
    ///
    /// Pourquoi l'ombre est dessinée par le PARENT : en UI Toolkit un élément se dessine
    /// avant ses enfants. Le parent du bouton dessine donc l'ombre (décalée de 8 px vers le
    /// bas), puis le bouton se dessine par-dessus : l'ombre est bien DERRIÈRE. Quand on appuie,
    /// le bouton descend de 6 px et l'ombre reste : elle ne dépasse plus que de 2 px.
    /// </summary>
    public static class ButtonStyler
    {
        public const string ButtonClass = "btn";
        /// Un bouton avec cette classe GARDE le focus clavier (« Jouer » : Entrée lance la partie).
        public const string KeepFocusClass = "keep-focus";
        /// Les gros boutons qui ont une ombre portée (Jouer, Encaisser, Rejouer, colonnes…).
        public const string ShadowClass = "btn--ombre";
        const string BounceClass = "btn--rebond";
        /// v6 : les bulles en image (colonnes, croix, roue, Rejouer, Continuer, Encaisser).
        public const string BubbleClass = "bulle";

        public static void Register(VisualElement root) => Apply(root);

        static readonly List<Button> buffer = new List<Button>();
        static readonly HashSet<VisualElement> shadowParents = new HashSet<VisualElement>();

        /// Stylise tous les boutons sous "root" (sans risque de le faire deux fois).
        public static void Apply(VisualElement root)
        {
            if (root == null) return;
            buffer.Clear();
            root.Query<Button>().ToList(buffer);
            foreach (var b in buffer) Style(b);
        }

        const string StyledClass = "btn-styled";

        static void Style(Button b)
        {
            // B2 : un bouton ne prend jamais le focus clavier (sauf exception voulue).
            if (!b.ClassListContains(KeepFocusClass)) b.focusable = false;
            // v6 (art bible §15.1.8) : une BULLE en image a déjà son reflet et son ombre dans le
            // PNG : on ne lui ajoute ni reflet ni ombre (sinon le « double reflet » revient).
            bool bubble = b.ClassListContains(BubbleClass);
            if (!bubble && b.ClassListContains(ShadowClass) && b.parent != null) RegisterShadowParent(b.parent);
            if (!b.ClassListContains(ButtonClass) || b.ClassListContains(StyledClass)) return;
            b.AddToClassList(StyledClass);
            if (bubble) { AddBounce(b); return; }
            // Le reflet est DESSINÉ (Painter2D) sur le bouton lui-même, et non ajouté comme
            // enfant : un enfant empêcherait le bouton de prendre la taille de son texte
            // (UI Toolkit ne mesure le texte que des éléments sans enfants).
            var reflet = new Reflet(b);
            b.generateVisualContent += reflet.Draw;
            b.RegisterCallback<PointerEnterEvent>(_ => { reflet.hovered = true; b.MarkDirtyRepaint(); });
            b.RegisterCallback<PointerLeaveEvent>(_ => { reflet.hovered = false; b.MarkDirtyRepaint(); });
            AddBounce(b);
        }

        /// Rebond au relâché (commun aux boutons classiques et aux bulles).
        static void AddBounce(Button b)
        {
            // Rebond au relâché : 1,12 pendant 0,07 s, puis retour au survol (1,08) avec le
            // ease-out-back du USS. TrickleDown : on le voit même si le bouton « mange » l'événement.
            b.RegisterCallback<PointerUpEvent>(_ =>
            {
                if (!b.enabledInHierarchy) return;
                b.AddToClassList(BounceClass);
                b.schedule.Execute(() => b.RemoveFromClassList(BounceClass)).StartingIn(70);
            }, TrickleDown.TrickleDown);
        }

        /// Le reflet d'un bouton (spec v4 §5.3, inchangé depuis v3 §10.4) : une bande blanche à
        /// 22 % sur le haut (40 % de la hauteur), coins 14 px, 35 % au survol, absente si le
        /// bouton est désactivé.
        class Reflet
        {
            readonly Button button;
            public bool hovered;
            public Reflet(Button b) => button = b;

            public void Draw(MeshGenerationContext ctx)
            {
                if (!button.enabledInHierarchy) return;
                Rect r = button.contentRect;
                // contentRect exclut les bordures et la marge intérieure : on repart du bord.
                float w = button.resolvedStyle.width - button.resolvedStyle.borderLeftWidth - button.resolvedStyle.borderRightWidth;
                float h = button.resolvedStyle.height - button.resolvedStyle.borderTopWidth - button.resolvedStyle.borderBottomWidth;
                if (float.IsNaN(w) || float.IsNaN(h) || w < 30f || h < 10f) return;
                float x0 = -button.resolvedStyle.paddingLeft + r.x + 8f, x1 = x0 + w - 16f;
                float y0 = -button.resolvedStyle.paddingTop + r.y + 3f, y1 = y0 + h * 0.40f;
                float rad = Mathf.Min(14f, (y1 - y0) * 0.5f, (x1 - x0) * 0.5f);
                var p = ctx.painter2D;
                p.fillColor = new Color(1f, 1f, 1f, hovered ? 0.35f : 0.22f);
                RoundedRect(p, x0, y0, x1, y1, rad);
                p.Fill();
            }
        }

        // =====================================================================
        // Ombre portée (dessinée par le parent, derrière les boutons .btn--ombre)
        // =====================================================================

        /// Donne l'ombre portée des gros boutons à n'importe quel élément (ex. le badge d'étage).
        public static void EnableShadow(VisualElement element)
        {
            element.AddToClassList(ShadowClass);
            if (element.parent != null) RegisterShadowParent(element.parent);
        }

        static void RegisterShadowParent(VisualElement parent)
        {
            if (!shadowParents.Add(parent)) return;
            parent.generateVisualContent += ctx => DrawShadows(parent, ctx);
            // Les boutons grossissent, apparaissent, glissent… : on redessine l'ombre ~30 fois
            // par seconde pour qu'elle suive (c'est très peu de travail).
            parent.schedule.Execute(parent.MarkDirtyRepaint).Every(33);
            parent.RegisterCallback<DetachFromPanelEvent>(_ => shadowParents.Remove(parent));
        }

        static void DrawShadows(VisualElement parent, MeshGenerationContext ctx)
        {
            var p = ctx.painter2D;
            foreach (var child in parent.Children())
            {
                if (!child.ClassListContains(ShadowClass) || child.ClassListContains(BubbleClass)) continue;
                var st = child.resolvedStyle;
                if (st.display == DisplayStyle.None || st.visibility == Visibility.Hidden || st.opacity <= 0.01f) continue;
                Rect l = child.layout;
                if (float.IsNaN(l.width) || l.width <= 0f) continue;
                // Même échelle que le bouton (autour de son centre) et même glissement horizontal
                // (colonnes qui arrivent), mais PAS sa descente à l'appui.
                float sx = st.scale.value.x, sy = st.scale.value.y;
                float cx = l.center.x + st.translate.x, cy = l.center.y + 8f;
                float w = l.width * sx, h = l.height * sy;
                float rad = Mathf.Min(st.borderTopLeftRadius * sx, w * 0.5f, h * 0.5f);
                float alpha = 0.35f * st.opacity * (child.enabledInHierarchy ? 1f : 0.6f) * OpacityOfParents(parent);
                p.fillColor = new Color(0f, 0f, 0f, alpha);
                RoundedRect(p, cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f, rad);
                p.Fill();
            }
        }

        static float OpacityOfParents(VisualElement e)
        {
            float o = 1f;
            for (; e != null; e = e.parent) o *= e.resolvedStyle.opacity;
            return o;
        }

        /// Un rectangle aux coins arrondis (chemin Painter2D).
        public static void RoundedRect(Painter2D p, float x0, float y0, float x1, float y1, float rad)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(x0 + rad, y0));
            p.LineTo(new Vector2(x1 - rad, y0));
            p.ArcTo(new Vector2(x1, y0), new Vector2(x1, y0 + rad), rad);
            p.LineTo(new Vector2(x1, y1 - rad));
            p.ArcTo(new Vector2(x1, y1), new Vector2(x1 - rad, y1), rad);
            p.LineTo(new Vector2(x0 + rad, y1));
            p.ArcTo(new Vector2(x0, y1), new Vector2(x0, y1 - rad), rad);
            p.LineTo(new Vector2(x0, y0 + rad));
            p.ArcTo(new Vector2(x0, y0), new Vector2(x0 + rad, y0), rad);
            p.ClosePath();
        }

        /// L'éclat qui passe sur « Jouer » : une bande blanche à 25 %, inclinée de 20°, qui
        /// traverse le bouton de gauche à droite en 0,5 s toutes les 3 s. Jamais sur un achat.
        /// Le texte « Jouer » passe dans un Label enfant (le bouton n'est plus une « feuille »).
        /// Renvoie l'élément ; c'est TickShine (appelé à chaque image) qui le fait avancer.
        public static VisualElement AddShine(Button b)
        {
            var label = new Label(b.text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("btn-label");
            b.text = "";
            b.Add(label);
            var shine = new VisualElement { name = "eclat", pickingMode = PickingMode.Ignore };
            shine.AddToClassList("btn-eclat");
            b.Add(shine);
            return shine;
        }

        /// Place l'éclat selon le temps (t en secondes depuis l'apparition du bouton).
        public static void TickShine(VisualElement shine, VisualElement button, float t)
        {
            if (shine == null) return;
            float width = button.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 0f) return;
            float phase = Mathf.Repeat(t, 3f);
            if (phase > 0.5f) { shine.style.opacity = 0f; return; }
            shine.style.opacity = 1f;
            shine.style.left = Mathf.Lerp(-60f, width + 20f, phase / 0.5f);
        }
    }
}
