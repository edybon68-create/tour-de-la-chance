using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Petits outils d'AFFICHAGE partagés par les pages du shop (Boutique, Inventaire, Index)
    /// et par l'écran d'ouverture : écrire un pourcentage « à la française », colorer un
    /// texte selon sa rareté, fabriquer une carte d'objet. Aucune règle de jeu ici : les
    /// nombres (chances, exemplaires…) viennent tous de la classe Shop.
    /// </summary>
    public static class ShopUi
    {
        static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

        /// v5.1 : les PICTOGRAMMES des monnaies dans un texte. La police n'a pas de joli
        /// diamant : on insère une petite image avec la balise « sprite » du texte enrichi.
        /// Les images viennent du Sprite Asset Assets/UI/Monnaies/monnaies.asset (diamant.png et
        /// piece.png, avec leur contour noir), réglé comme « Default Sprite Asset » dans
        /// Assets/UI/TexteReglages.asset (les réglages de texte de HudPanelSettings).
        /// Exemple : $"Continuer — 1 {ShopUi.Diamant}".
        public const string Diamant = "<sprite name=\"diamant\">";
        public const string Piece = "<sprite name=\"piece\">";

        /// Un pourcentage avec 2 décimales au plus, sans zéros inutiles : 10 %, 6,25 %, 4,67 %.
        public static string Percent(double value) => value.ToString("0.##", French) + "\u00A0%"; // v6 (QA n°41) : espace INSÉCABLE, « % » ne part jamais seul à la ligne

        /// Un nombre « à la française », sans zéros inutiles : 1,5 ; 3 ; 1,13.
        public static string Number(float value) => value.ToString("0.##", French);

        /// Un entier avec une espace entre les milliers : 12 340. (Espace ordinaire : la police
        /// n'a pas l'espace fine insécable que .NET utilise pour le français.)
        public static string Thousands(int value) =>
            value.ToString("#,0", CultureInfo.InvariantCulture).Replace(",", " ");

        /// Couleur « texte » d'une rareté (plus claire pour Rare et Super rare, art-bible §10.2).
        public static void ColorText(VisualElement label, RarityDef rarity)
        {
            if (rarity == null) return;
            label.style.color = rarity.textColor;
            label.AddToClassList("rarete-" + rarity.id); // même identifiant que le catalogue
        }

        /// Liseré de 3 px de la couleur de base de la rareté.
        public static void ColorBorder(VisualElement element, RarityDef rarity)
        {
            if (rarity == null) return;
            var c = rarity.color;
            element.style.borderTopColor = c;
            element.style.borderBottomColor = c;
            element.style.borderLeftColor = c;
            element.style.borderRightColor = c;
        }

        public static void SetClass(VisualElement element, string className, bool on)
        {
            if (on) element.AddToClassList(className);
            else element.RemoveFromClassList(className);
        }

        /// v6 (QA n°41) : remplace l'espace ordinaire avant « % » par une espace INSÉCABLE
        /// (U+00A0, présente dans la police Fredoka) : le retour à la ligne ne sépare plus
        /// « +4 » de « % ». Sert aussi pour les textes qui viennent des catalogues.
        public static string NoBreakPercent(string text) =>
            string.IsNullOrEmpty(text) ? text : text.Replace(" %", "\u00A0%");

        public static Label NewLabel(string text, string className)
        {
            var label = new Label(NoBreakPercent(text)) { pickingMode = PickingMode.Ignore };
            // Plusieurs classes possibles, séparées par une espace (« missing btq-missing »).
            foreach (var c in className.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) label.AddToClassList(c);
            return label;
        }

        /// v5 : le substitut d'icône (jeton rond couleur de rareté, initiale, petit liseré clair)
        /// tant que l'image de l'objet n'existe pas. Pur affichage.
        public static VisualElement Placeholder(ShopItem item)
        {
            var token = new VisualElement { pickingMode = PickingMode.Ignore };
            token.AddToClassList("item-placeholder");
            var rarity = Shop.RarityOf(item);
            Color c = rarity != null ? rarity.color : new Color(0.4f, 0.38f, 0.5f);
            token.style.backgroundColor = Color.Lerp(c, item.Color, 0.35f);
            token.style.borderTopColor = Color.Lerp(c, Color.white, 0.5f);
            token.style.borderLeftColor = Color.Lerp(c, Color.white, 0.5f);
            token.style.borderRightColor = Color.Lerp(c, Color.black, 0.3f);
            token.style.borderBottomColor = Color.Lerp(c, Color.black, 0.3f);
            string name = item.DisplayName ?? "?";
            token.Add(NewLabel(name.Length > 0 ? name.Substring(0, 1).ToUpperInvariant() : "?", "item-placeholder__lettre"));
            return token;
        }

        /// v5 §8.3 : la CARTE DE COLLECTION (Index, « Contenu et taux ») : icône en couleurs (ou
        /// « ? » pour un Secret non obtenu), nom, liseré de rareté, chance par objet, bonus de
        /// tenue, mini-image de la boîte d'origine, badge « ×3 », coin vert « ✓ » si obtenu,
        /// ruban gris « Pas encore » sinon.
        /// v6 §6.1 : hideUnowned = vrai dans l'Index : un objet non obtenu (non Secret) montre son
        /// NOM mais pas son icône (pictogramme « pas encore vu » teinté de sa rareté). Dans
        /// « Contenu et taux » (Boutique), faux : tout est en couleurs sauf les Secrets non obtenus.
        public static Button CollectionTile(ShopItem item, Action onClick, bool hideUnowned = false)
        {
            int count = Shop.CountOf(item);
            bool owned = count >= 1;
            bool mystery = !owned && item.RarityId == "secret";
            bool notSeen = hideUnowned && !owned && !mystery;
            var frame = new Button(onClick);
            frame.AddToClassList("item-frame");
            frame.AddToClassList("tile-frame");
            var inner = new VisualElement { pickingMode = PickingMode.Ignore };
            inner.AddToClassList("item-card");
            inner.AddToClassList("tile-card");
            if (!owned) inner.AddToClassList("tile-card--absent");
            frame.Add(inner);
            var rarity = Shop.RarityOf(item);
            ColorBorder(inner, rarity);

            // Mini-image de la boîte d'origine (20 px, coin haut gauche).
            var box = Shop.BoxOf(item);
            var mini = new VisualElement { pickingMode = PickingMode.Ignore };
            mini.AddToClassList("tile-box");
            // V7.1 n°12 : un objet jamais vu porte, dans ce coin, le pictogramme « pas encore vu ».
            if (notSeen && UiImages.PasEncoreVu != null) { mini.AddToClassList("tile-box--pas-vu"); mini.style.backgroundImage = new StyleBackground(UiImages.PasEncoreVu); }
            else if (box != null && box.icon != null) mini.style.backgroundImage = new StyleBackground(box.icon);
            else if (box != null) { mini.AddToClassList("tile-box--vide"); mini.style.backgroundColor = box.openingTint; }
            inner.Add(mini);

            if (notSeen)
            {
                var holder = new VisualElement { pickingMode = PickingMode.Ignore };
                holder.AddToClassList("item-visual");
                holder.style.alignItems = Align.Center;
                holder.style.justifyContent = Justify.Center;
                holder.Add(NotSeenPicto(rarity, 88f));
                inner.Add(holder);
            }
            else inner.Add(Visual(item, mystery ? Look.Mystery : Look.Normal));
            inner.Add(NewLabel(mystery ? "???" : item.DisplayName, "item-name"));
            inner.Add(NewLabel(Percent(Shop.ItemChance(item)), "item-extra"));
            int bonus = Shop.BonusPercent(item);
            if (bonus > 0) inner.Add(NewLabel($"+{bonus}\u00A0%", "bonus-badge"));
            if (count >= 2) inner.Add(NewLabel("×" + count, "item-badge"));
            // V7.1 n°12 : plus de pilule « Pas encore » (la silhouette et le nom à 75 % le disent).
            if (owned) inner.Add(NewLabel("✓", "tile-check"));
            return frame;
        }

        /// Ce que montre une case de l'Index pour un objet non obtenu.
        public enum Look
        {
            Normal,      // l'icône en couleurs
            Silhouette,  // l'icône teintée sombre (non obtenu)
            Mystery      // « ? » (Secret non obtenu)
        }

        /// <summary>
        /// Fabrique une carte d'objet : un cadre cliquable (contour or si sélectionné) qui
        /// contient la carte (liseré de rareté), le visuel, le nom et la rareté.
        /// Renvoie la carte intérieure dans « inner » pour ajouter des lignes en plus.
        /// </summary>
        public static Button ItemCard(ShopItem item, Look look, bool selected, Action onClick, out VisualElement inner,
                                      string numberText = null)
        {
            var frame = new Button(onClick);
            frame.AddToClassList("item-frame");
            SetClass(frame, "item-frame--selected", selected);

            inner = new VisualElement { pickingMode = PickingMode.Ignore };
            inner.AddToClassList("item-card");
            frame.Add(inner);

            var rarity = Shop.RarityOf(item);
            ColorBorder(inner, rarity);

            if (numberText != null) inner.Add(NewLabel(numberText, "item-number"));
            inner.Add(Visual(item, look));
            inner.Add(NewLabel(look == Look.Mystery ? "???" : item.DisplayName, "item-name"));

            var rarityLabel = NewLabel(rarity != null ? rarity.displayName : "Gratuit", "item-rarity");
            if (rarity != null) ColorText(rarityLabel, rarity);
            else rarityLabel.style.color = new Color(0.66f, 0.64f, 0.72f);
            inner.Add(rarityLabel);
            return frame;
        }

        /// Le visuel d'un objet, dans un carré de taille fixe (classe item-visual).
        public static VisualElement Visual(ShopItem item, Look look, string className = "item-visual")
        {
            var box = new VisualElement { pickingMode = PickingMode.Ignore };
            box.AddToClassList(className);

            if (look == Look.Mystery)
            {
                box.Add(NewLabel("?", "item-question"));
            }
            else if (item.Icon != null)
            {
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("item-icon");
                icon.style.backgroundImage = new StyleBackground(item.Icon);
                if (look == Look.Silhouette) icon.AddToClassList("item-icon--silhouette");
                box.Add(icon);
            }
            else if (!item.IsFree && item.Category == ShopCategory.Accessoire)
            {
                // v5 : un accessoire dont l'icône n'existe pas encore (art de la partie 2) : un
                // SUBSTITUT PROPRE, un jeton de la couleur de sa rareté avec l'initiale de l'objet.
                box.Add(Placeholder(item));
            }
            else if (item.Category == ShopCategory.Accessoire)
            {
                // Pas d'icône pour un accessoire = « Rien » : un cercle barré en USS.
                var circle = new VisualElement { pickingMode = PickingMode.Ignore };
                circle.AddToClassList("item-none");
                var bar = new VisualElement { pickingMode = PickingMode.Ignore };
                bar.AddToClassList("item-none-bar");
                circle.Add(bar);
                box.Add(circle);
            }
            else
            {
                // v6 (QA n°49) : secours pour un skin sans icône. Avant : un carré uni. Maintenant
                // un vrai substitut, comme pour les accessoires : un jeton rond « autocollant »
                // (contour d'encre) rempli de la couleur du skin, avec l'initiale du skin.
                var token = Placeholder(item);
                token.AddToClassList("item-placeholder--skin");
                token.style.backgroundColor = look == Look.Silhouette ? new Color(0.08f, 0.07f, 0.12f) : item.Color;
                var ink = new Color(0.067f, 0.051f, 0.122f); // #110D1F
                token.style.borderTopColor = ink;
                token.style.borderLeftColor = ink;
                token.style.borderRightColor = ink;
                token.style.borderBottomColor = ink;
                box.Add(token);
            }
            return box;
        }

        // =====================================================================
        // v6 §6.1 : le pictogramme « objet pas encore vu » (Index)
        // =====================================================================

        /// Le pictogramme « pas encore vu », LE MÊME pour tous les objets, teinté de la couleur de
        /// la rareté : un jeton rond contouré d'encre avec un œil fermé. Dessiné par le code (en
        /// attendant un dessin du DA) : AUCUN pixel de l'icône ni du modèle de l'objet n'est montré.
        public static VisualElement NotSeenPicto(RarityDef rarity, float size)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("pas-vu");
            e.style.width = size;
            e.style.height = size;
            // v7 (point 10) : l'image dessinée par le DA (pas_encore_vu.png) si elle est là ; sinon
            // le jeton dessiné par le code, teinté de la rareté (repli).
            var picture = UiImages.PasEncoreVu;
            if (picture != null)
            {
                e.style.backgroundImage = new StyleBackground(picture);
                e.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
                return e;
            }
            Color tint = rarity != null ? rarity.color : new Color(0.6f, 0.6f, 0.7f);
            e.generateVisualContent += ctx => DrawNotSeen(ctx, tint);
            return e;
        }

        static void DrawNotSeen(MeshGenerationContext ctx, Color tint)
        {
            var r = ctx.visualElement.contentRect;
            if (r.width <= 0f) return;
            var p = ctx.painter2D;
            Vector2 c = r.center;
            float rad = Mathf.Min(r.width, r.height) * 0.44f;
            var ink = new Color(0.067f, 0.051f, 0.122f);
            // Le jeton : la couleur de la rareté, un peu assombrie en bas (relief).
            p.fillColor = Color.Lerp(tint, Color.black, 0.15f);
            p.BeginPath(); p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(360f)); p.Fill();
            p.fillColor = Color.Lerp(tint, Color.white, 0.15f);
            p.BeginPath(); p.Arc(c + new Vector2(0f, -rad * 0.08f), rad * 0.86f, Angle.Degrees(0f), Angle.Degrees(360f)); p.Fill();
            p.strokeColor = ink; p.lineWidth = Mathf.Max(3f, rad * 0.1f);
            p.BeginPath(); p.Arc(c, rad, Angle.Degrees(0f), Angle.Degrees(360f)); p.Stroke();
            // L'œil fermé : un arc blanc contouré, et trois cils.
            float w = rad * 0.55f;
            foreach (var (width, col) in new[] { (rad * 0.2f, ink), (rad * 0.1f, Color.white) })
            {
                p.strokeColor = col; p.lineWidth = width; p.lineCap = LineCap.Round;
                p.BeginPath(); p.Arc(c + new Vector2(0f, -rad * 0.15f), w, Angle.Degrees(30f), Angle.Degrees(150f)); p.Stroke();
                for (int k = -1; k <= 1; k++)
                {
                    float a = (90f + k * 40f) * Mathf.Deg2Rad;
                    Vector2 from = c + new Vector2(0f, -rad * 0.15f) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * w;
                    p.BeginPath(); p.MoveTo(from); p.LineTo(from + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad * 0.22f); p.Stroke();
                }
            }
        }

        /// Un chevron ◀ ou ▶ dessiné par le code (flèches de la fiche agrandie, art bible §15.6) :
        /// trait blanc contouré d'encre, avec sa copie « épaisseur » dessous.
        public static VisualElement Chevron(bool right)
        {
            var e = new VisualElement { pickingMode = PickingMode.Ignore };
            e.AddToClassList("chevron");
            e.generateVisualContent += ctx =>
            {
                var r = ctx.visualElement.contentRect;
                if (r.width <= 0f) return;
                var p = ctx.painter2D;
                Vector2 c = new Vector2(r.width * 0.5f, r.height * 0.5f); // centre de la face de la bulle
                float s = right ? 1f : -1f;
                void Path(Vector2 off)
                {
                    p.BeginPath();
                    p.MoveTo(c + off + new Vector2(-8f * s, -16f));
                    p.LineTo(c + off + new Vector2(10f * s, 0f));
                    p.LineTo(c + off + new Vector2(-8f * s, 16f));
                    p.Stroke();
                }
                p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
                p.strokeColor = new Color(0.18f, 0.165f, 0.306f); p.lineWidth = 18f; Path(new Vector2(0f, 3f)); // épaisseur #2E2A4E
                p.strokeColor = new Color(0.118f, 0.102f, 0.2f); p.lineWidth = 18f; Path(Vector2.zero);         // contour #1E1A33
                p.strokeColor = Color.white; p.lineWidth = 11f; Path(Vector2.zero);
            };
            return e;
        }
    }
}
