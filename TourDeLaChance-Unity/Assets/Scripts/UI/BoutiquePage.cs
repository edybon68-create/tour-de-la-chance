using System;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// Le panneau « Boutique » (spec boites §7.3, prix v3 : 250) : une carte par boîte, avec
    /// son image, son prix, la progression de la collection, le tableau des taux (lu dans le
    /// catalogue) et le bouton « Ouvrir ». Ce n'est pas un MonoBehaviour : un simple objet
    /// créé et piloté par PanelsController, comme InventoryPage et IndexPage.
    /// Il AFFICHE et TRANSMET les clics ; ouvrir une boîte est décidé par la classe Shop.
    /// </summary>
    public class BoutiquePage
    {
        readonly ScrollView list;

        /// Clic sur « Ouvrir » : PanelsController transmet à Shop.TryOpenBox.
        public event Action<LootBox> OpenRequested;
        /// « Voir le contenu » : PanelsController ouvre l'Index sur cette boîte.
        public event Action<LootBox> ShowContentRequested;

        public BoutiquePage(ScrollView page) => list = page;

        public void Open()
        {
            Refresh();
            list.scrollOffset = UnityEngine.Vector2.zero;
        }

        public void Refresh()
        {
            list.Clear();
            foreach (var box in Shop.Boxes) list.Add(BoxCard(box));
            ButtonStyler.Apply(list);
        }

        VisualElement BoxCard(LootBox box)
        {
            var card = new VisualElement();
            card.AddToClassList("box-card");

            // 1. Image + nom + prix
            var top = new VisualElement { pickingMode = PickingMode.Ignore };
            top.AddToClassList("box-top");
            var image = new VisualElement { pickingMode = PickingMode.Ignore };
            image.AddToClassList("box-image");
            if (box.icon != null) image.style.backgroundImage = new StyleBackground(box.icon);
            top.Add(image);
            var titleCol = new VisualElement { pickingMode = PickingMode.Ignore };
            titleCol.AddToClassList("box-title-col");
            titleCol.Add(ShopUi.NewLabel(box.displayName, "box-name"));
            var priceRow = new VisualElement { pickingMode = PickingMode.Ignore };
            priceRow.AddToClassList("price-row");
            priceRow.Add(ShopUi.NewLabel(box.price.ToString(), "price-text"));
            var coin = new VisualElement { pickingMode = PickingMode.Ignore };
            coin.AddToClassList("coin");
            coin.AddToClassList("coin--small");
            priceRow.Add(coin);
            titleCol.Add(priceRow);
            top.Add(titleCol);
            card.Add(top);

            // 2. Progression
            int found = Shop.FoundCount(box), total = box.items.Count;
            var progressRow = new VisualElement { pickingMode = PickingMode.Ignore };
            progressRow.AddToClassList("progress-row");
            progressRow.Add(ShopUi.NewLabel($"Collection : {found} / {total}", "progress-text"));
            var bar = new VisualElement { pickingMode = PickingMode.Ignore };
            bar.AddToClassList("progress-bar");
            var fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("progress-fill");
            fill.style.width = Length.Percent(total > 0 ? 100f * found / total : 0f);
            bar.Add(fill);
            progressRow.Add(bar);
            card.Add(progressRow);

            // 3. Tableau des taux : TOUT est lu dans le catalogue (aucun chiffre en dur).
            card.Add(RatesTable(box));
            card.Add(ShopUi.NewLabel("Chaque ouverture donne 1 objet. Les chances sont toujours les mêmes, à chaque ouverture.", "rates-note"));

            // Collection complète : la boîte reste ouvrable.
            if (Shop.IsComplete(box))
                card.Add(ShopUi.NewLabel("Collection complète ! Chaque ouverture donnera un doublon.", "complete-banner"));

            // 4. Bouton « Ouvrir — 250 » (grisé + « Il te manque X pièces » si pas assez).
            var buttons = new VisualElement();
            buttons.AddToClassList("box-buttons");
            var openButton = new Button(() => OpenRequested?.Invoke(box)) { text = $"Ouvrir — {box.price}" };
            openButton.AddToClassList("btn");
            openButton.AddToClassList("btn--vert");
            openButton.AddToClassList("open-btn");
            openButton.AddToClassList(UiSounds.SilentClass); // le son de paiement suffit
            bool canOpen = Shop.CanOpen(box);
            openButton.SetEnabled(canOpen);
            buttons.Add(openButton);
            if (!canOpen) buttons.Add(ShopUi.NewLabel($"Il te manque {Shop.MissingCoins(box)} pièces", "missing"));
            card.Add(buttons);

            var link = new Button(() => ShowContentRequested?.Invoke(box)) { text = "Voir le contenu" };
            link.AddToClassList("link-btn");
            card.Add(link);
            return card;
        }

        VisualElement RatesTable(LootBox box)
        {
            var table = new VisualElement { pickingMode = PickingMode.Ignore };
            table.AddToClassList("rates");
            table.Add(RateRow(null, "Rareté", "Chance", "Objets", header: true));
            foreach (var rarity in Shop.Catalog.Rarities)
            {
                int count = Shop.ItemsOfRarity(box, rarity.id).Count;
                table.Add(RateRow(rarity, rarity.displayName, ShopUi.Percent(Shop.RarityChance(rarity)),
                                  count.ToString(), header: false));
            }
            return table;
        }

        static VisualElement RateRow(RarityDef rarity, string name, string chance, string count, bool header)
        {
            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("rate-row");
            if (header) row.AddToClassList("rate-row--header");

            var dotCell = new VisualElement { pickingMode = PickingMode.Ignore };
            dotCell.AddToClassList("rate-dot-cell");
            if (rarity != null)
            {
                var dot = new VisualElement { pickingMode = PickingMode.Ignore };
                dot.AddToClassList("rate-dot");
                dot.style.backgroundColor = rarity.color; // pastille : couleur de base
                dotCell.Add(dot);
            }
            row.Add(dotCell);

            var nameLabel = ShopUi.NewLabel(name, "rate-cell");
            nameLabel.AddToClassList("rate-name");
            if (rarity != null) ShopUi.ColorText(nameLabel, rarity); // nom : couleur « texte »
            row.Add(nameLabel);
            var chanceLabel = ShopUi.NewLabel(chance, "rate-cell");
            chanceLabel.AddToClassList("rate-chance");
            row.Add(chanceLabel);
            var countLabel = ShopUi.NewLabel(count, "rate-cell");
            countLabel.AddToClassList("rate-count");
            row.Add(countLabel);
            return row;
        }
    }
}
