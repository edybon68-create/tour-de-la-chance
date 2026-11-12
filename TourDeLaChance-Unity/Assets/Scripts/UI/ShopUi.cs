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
        public static string Percent(double value) => value.ToString("0.##", French) + " %";

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

        public static Label NewLabel(string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
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
        public static Button CollectionTile(ShopItem item, Action onClick)
        {
            int count = Shop.CountOf(item);
            bool owned = count >= 1;
            bool mystery = !owned && item.RarityId == "secret";
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
            if (box != null && box.icon != null) mini.style.backgroundImage = new StyleBackground(box.icon);
            else if (box != null) { mini.AddToClassList("tile-box--vide"); mini.style.backgroundColor = box.openingTint; }
            inner.Add(mini);

            inner.Add(Visual(item, mystery ? Look.Mystery : Look.Normal));
            inner.Add(NewLabel(mystery ? "???" : item.DisplayName, "item-name"));
            inner.Add(NewLabel(Percent(Shop.ItemChance(item)), "item-extra"));
            int bonus = Shop.BonusPercent(item);
            if (bonus > 0) inner.Add(NewLabel($"+{bonus} %", "bonus-badge"));
            if (count >= 2) inner.Add(NewLabel("×" + count, "item-badge"));
            if (owned) inner.Add(NewLabel("✓", "tile-check"));
            else inner.Add(NewLabel("Pas encore", "tile-ribbon"));
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
                // Secours pour un skin sans icône : un carré de sa couleur.
                var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
                swatch.AddToClassList("item-swatch");
                swatch.style.backgroundColor = look == Look.Silhouette ? new Color(0.08f, 0.07f, 0.12f) : item.Color;
                box.Add(swatch);
            }
            return box;
        }
    }
}
