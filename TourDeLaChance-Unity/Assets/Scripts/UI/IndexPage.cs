using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace TourDeLaChance
{
    /// <summary>
    /// L'onglet « Index » du shop (spec boites §7.5) : toute la collection, boîte par boîte,
    /// pour voir ce qui reste à trouver. 16 cases par boîte, dans l'ordre des numéros.
    /// Un objet non obtenu est en silhouette (le Secret affiche « ? » et « ??? ») ; cliquer
    /// une case non obtenue ne montre RIEN sur le personnage (la surprise reste entière).
    /// Simple objet piloté par PanelsController ; aucune règle de jeu ici.
    /// </summary>
    public class IndexPage : IDisposable
    {
        const string SecretId = "secret";

        readonly VisualElement tabsRow, barFill;
        readonly ScrollView grid;
        readonly Label progress, info;
        readonly Button gotoButton;

        public event Action<ShopItem> PreviewRequested;
        /// « Voir dans l'inventaire » : le ShopController bascule sur l'Inventaire.
        public event Action<ShopItem> ShowInInventoryRequested;

        LootBox box;
        ShopItem selected;

        public IndexPage(VisualElement page)
        {
            tabsRow = page.Q<VisualElement>("idx-tabs");
            barFill = page.Q<VisualElement>("idx-bar-fill");
            grid = page.Q<ScrollView>("idx-grid");
            progress = page.Q<Label>("idx-progress");
            info = page.Q<Label>("idx-info");
            gotoButton = page.Q<Button>("idx-goto");
            gotoButton.clicked += OnGoto;
        }

        void OnGoto() { if (selected != null) ShowInInventoryRequested?.Invoke(selected); }

        /// Correctif B9 : se désabonner de ce qu'on a branché.
        public void Dispose() => gotoButton.clicked -= OnGoto;

        /// Ouvre l'Index sur une boîte (la première par défaut).
        public void Open(LootBox focus = null)
        {
            box = focus ?? (Shop.Boxes.Count > 0 ? Shop.Boxes[0] : null);
            selected = null;
            Refresh();
            grid.scrollOffset = Vector2.zero;
        }

        public void Refresh()
        {
            // Un sous-onglet par boîte du catalogue.
            tabsRow.Clear();
            foreach (var b in Shop.Boxes)
            {
                var target = b;
                var tab = new Button(() => { box = target; selected = null; Refresh(); }) { text = b.displayName };
                tab.AddToClassList("btn");
                tab.AddToClassList("btn--sombre");
                tab.AddToClassList("subtab");
                ShopUi.SetClass(tab, "subtab--active", b == box);
                tabsRow.Add(tab);
            }

            grid.Clear();
            if (box == null) return;

            int found = Shop.FoundCount(box), total = box.items.Count;
            progress.text = $"{found} / {total} trouvés";
            barFill.style.width = Length.Percent(total > 0 ? 100f * found / total : 0f);

            var cells = new VisualElement { pickingMode = PickingMode.Ignore };
            cells.AddToClassList("grid");
            for (int i = 0; i < box.items.Count; i++)
            {
                var item = box.items[i];
                if (item == null) continue;
                cells.Add(Cell(item, i + 1));
            }
            grid.Add(cells);
            RefreshInfo();
            ButtonStyler.Apply(tabsRow);
            ButtonStyler.Apply(grid);
        }

        VisualElement Cell(ShopItem item, int number)
        {
            bool owned = Shop.CountOf(item) >= 1;
            var look = owned ? ShopUi.Look.Normal
                     : item.RarityId == SecretId ? ShopUi.Look.Mystery
                     : ShopUi.Look.Silhouette;
            var card = ShopUi.ItemCard(item, look, item == selected, () => Select(item), out var inner,
                                       "n°" + number.ToString("00"));
            // Chance par objet, calculée par la logique à partir du catalogue.
            inner.Add(ShopUi.NewLabel(ShopUi.Percent(Shop.ItemChance(item)), "item-extra"));
            return card;
        }

        void Select(ShopItem item)
        {
            selected = item;
            // Aperçu seulement pour un objet obtenu (spec §7.5, critère 25).
            if (Shop.CountOf(item) >= 1) PreviewRequested?.Invoke(item);
            Refresh();
        }

        void RefreshInfo()
        {
            if (selected == null)
            {
                info.text = "Clique une case pour la regarder de plus près.";
                gotoButton.style.display = DisplayStyle.None;
                return;
            }
            bool owned = Shop.CountOf(selected) >= 1;
            if (owned)
            {
                info.text = $"{selected.DisplayName} — tu en as {Shop.CountOf(selected)}.";
                gotoButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                info.text = $"Pas encore trouvé. Il est dans la {box.displayName}.";
                gotoButton.style.display = DisplayStyle.None;
            }
        }
    }
}
