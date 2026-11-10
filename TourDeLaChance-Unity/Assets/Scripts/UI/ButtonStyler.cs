using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Petits outils d'AFFICHAGE pour les boutons « bonbons » (spec v3 §10.4), sur le modèle
    /// de UiSounds.Register : on les branche UNE fois sur la racine d'un écran.
    ///
    /// - Register(root) / Apply(élément) : ajoute le REFLET (un petit élément blanc en haut)
    ///   dans chaque bouton de classe .btn. Les écrans qui recréent des cartes par le code
    ///   rappellent Apply sur le contenu recréé. Il retire aussi le focus clavier des boutons
    ///   (correctif B2) : Espace et Entrée n'« appuient » plus sur un bouton cliqué avant.
    /// - AddShine(button) : l'éclat qui passe, réservé au bouton « Jouer ».
    /// Le style lui-même (biseau, épaisseur, états) est dans Assets/UI/Buttons.uss.
    /// </summary>
    public static class ButtonStyler
    {
        public const string ButtonClass = "btn";
        /// Un bouton avec cette classe GARDE le focus clavier (« Jouer » : Entrée lance la partie).
        public const string KeepFocusClass = "keep-focus";

        public static void Register(VisualElement root) => Apply(root);

        static readonly List<Button> buffer = new List<Button>();

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
            if (!b.ClassListContains(ButtonClass) || b.ClassListContains(StyledClass)) return;
            b.AddToClassList(StyledClass);
            // Le reflet est DESSINÉ (Painter2D) sur le bouton lui-même, et non ajouté comme
            // enfant : un enfant empêcherait le bouton de prendre la taille de son texte
            // (UI Toolkit ne mesure le texte que des éléments sans enfants).
            var reflet = new Reflet(b);
            b.generateVisualContent += reflet.Draw;
            b.RegisterCallback<PointerEnterEvent>(_ => { reflet.hovered = true; b.MarkDirtyRepaint(); });
            b.RegisterCallback<PointerLeaveEvent>(_ => { reflet.hovered = false; b.MarkDirtyRepaint(); });
        }

        /// Le reflet d'un bouton : un rectangle arrondi blanc en haut (4 px du haut, 10 px des
        /// côtés, 38 % de la hauteur), à 18 % (30 % au survol), absent si le bouton est désactivé.
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
                float x0 = -button.resolvedStyle.paddingLeft + r.x + 10f, x1 = x0 + w - 20f;
                float y0 = -button.resolvedStyle.paddingTop + r.y + 4f, y1 = y0 + h * 0.38f;
                float rad = Mathf.Min(12f, (y1 - y0) * 0.5f, (x1 - x0) * 0.5f);
                var p = ctx.painter2D;
                p.fillColor = new Color(1f, 1f, 1f, hovered ? 0.30f : 0.18f);
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
                p.Fill();
            }
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
