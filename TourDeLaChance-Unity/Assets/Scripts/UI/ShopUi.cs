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

        /// Un pourcentage avec 2 décimales au plus, sans zéros inutiles : 10 %, 6,25 %, 4,67 %.
        public static string Percent(double value) => value.ToString("0.##", French) + " %";

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
            label.AddToClassList(className);
            return label;
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
